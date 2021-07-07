using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Komponent.IO.Attributes;
using Kontract.Models.Text;
using Kontract.Models.Text.ControlCodeProcessor;
using Kontract.Models.Text.TextPager;
using Kryptography;

namespace plugin_mt_framework.Text
{
    class GmdHeader
    {
        [FixedLength(4)]
        public string magic;
        public int version;
        public Language language;
        public long zero1;
        public int labelCount;
        public int sectionCount;
        public int labelSize;
        public int sectionSize;
        public int nameSize;
    }

    class GmdEntryV1
    {
        public int sectionId;
        public uint labelOffset;
    }

    class GmdEntryV2
    {
        public int sectionId;
        public uint hash1;
        public uint hash2;
        public uint labelOffset;
        public int listLink;
    }

    class GmdEntryV2Mobile
    {
        public int sectionId;
        public uint hash1;
        public uint hash2;
        public int zero1;
        public long labelOffset;
        public long listLink;
    }

    public enum Language
    {
        Japanese,
        English,
        French,
        Spanish,
        German,
        Italian
    }

    static class GmdSupport
    {
        private static List<string> key1 = new List<string> { "fjfajfahajra;tira9tgujagjjgajgoa", "e43bcc7fcab+a6c4ed22fcd433/9d2e6cb053fa462-463f3a446b19" };
        private static List<string> key2 = new List<string> { "mva;eignhpe/dfkfjgp295jtugkpejfu", "861f1dca05a0;9ddd5261e5dcc@6b438e6c.8ba7d71c*4fd11f3af1" };

        public static bool IsEncrypted(Stream input)
        {
            // Get last byte
            var lastByte = PeekLastByte(input);

            // Shortcut: If last byte is 0 and key does not produce 0 at that position, we return the stream as is, because it is not encrypted
            if (lastByte == 0)
            {
                for (var i = 0; i < key1.Count; i++)
                {
                    var keyPos = (int)(input.Length - 1) % key1[i].Length;
                    if (key1[i][keyPos] != key2[i][keyPos])
                        return false;
                }
            }

            return true;
        }

        public static int DetermineKeyIndex(Stream input)
        {
            // Get last byte
            var lastByte = PeekLastByte(input);

            // Determine key pair index
            var keyIndex = -1;
            for (var i = 0; i < key1.Count; i++)
            {
                var keyPos = (int)(input.Length - 1) % key1[i].Length;
                if ((lastByte ^ key1[i][keyPos] ^ key2[i][keyPos]) == 0)
                {
                    keyIndex = i;
                    break;
                }
            }

            return keyIndex;
        }

        public static Stream WrapXor(Stream input, int keyIndex)
        {
            if (keyIndex < 0)
                throw new InvalidOperationException("Unknown key pair for GMD content.");

            // Create key
            var key = new byte[key1[keyIndex].Length];
            for (var i = 0; i < key1[keyIndex].Length; i++)
                key[i] = (byte)(key1[keyIndex][i] ^ key2[keyIndex][i]);

            // Wrap into XOR stream
            return new XorStream(input, key);
        }

        private static int PeekLastByte(Stream input)
        {
            var bkPos = input.Position;
            input.Position = input.Length - 1;
            var lastByte = input.ReadByte();
            input.Position = bkPos;

            return lastByte;
        }
    }

    class GmdControlCodeProcessor : IControlCodeProcessor
    {
        public ProcessedText Read(byte[] data, Encoding encoding)
        {
            var elements = new List<ProcessedElement>();

            var decodedText = encoding.GetString(data);

            var partText = string.Empty;
            for (var i = 0; i < decodedText.Length; i++)
            {
                if (IsControlCode(decodedText, i))
                {
                    if (!string.IsNullOrEmpty(partText))
                    {
                        elements.Add(partText);
                        partText = string.Empty;
                    }

                    var code = ReadControlCode(decodedText, i, out var endIndex);
                    elements.Add(code);

                    i += endIndex - i - 1;

                    continue;
                }

                partText += decodedText[i];
            }

            if (!string.IsNullOrEmpty(partText))
                elements.Add(partText);

            return new ProcessedText(elements);
        }

        public byte[] Write(ProcessedText text, Encoding encoding)
        {
            // TODO
            return Array.Empty<byte>();
        }

        private bool IsControlCode(string text, int index)
        {
            return text[index] == '<';
        }

        private ControlCode ReadControlCode(string text, int index, out int endIndex)
        {
            endIndex = -1;

            // Check that enough text is available for a minimum viable control code
            if (text.Length - index < 6)
                return null;

            // Skip beginning <
            index++;


            // Take everything until > as control code name
            var codeIdLength = CalculateLengthUntil(text, index, '>', ' ');
            var codeId = text.Substring(index, codeIdLength);
            index += codeIdLength;

            // Check if code already ends
            if (text[index] == '>')
            {
                endIndex = index + 1;
                return new ControlCode(codeId);
            }

            // Otherwise, skip following space
            index++;

            // Read until control code part ends
            var remainingLength = CalculateLengthUntil(text, index, '>');

            var args = text.Substring(index, remainingLength);
            endIndex = index + remainingLength + 1;

            return new ControlCode(codeId, args.Split(' '));
        }

        private int CalculateLengthUntil(string text, int index, params char[] until)
        {
            var length = 0;
            for (var i = index; i < text.Length; i++, length++)
                if (until.Contains(text[i]))
                    break;

            return length;
        }
    }

    class GmdTextPager : ITextPager
    {
        public IList<ProcessedText> Split(ProcessedText text)
        {
            var result = new List<ProcessedText>();

            var elements = new List<ProcessedElement>();
            foreach (var element in text.Elements)
            {
                if (element.IsControlCode && element.GetControlCode().Name == "PAGE")
                {
                    result.Add(new ProcessedText(elements.ToArray()));
                    elements = new List<ProcessedElement>();

                    continue;
                }

                elements.Add(element);
            }

            if (elements.Count > 0)
                result.Add(new ProcessedText(elements));

            return result;
        }

        public ProcessedText Merge(IList<ProcessedText> texts)
        {
            var elements = new List<ProcessedElement>();

            for (var i = 0; i < texts.Count; i++)
            {
                var text = texts[i];
                elements.AddRange(text.Elements);

                if (i + 1 < texts.Count)
                    elements.Add(new ControlCode("PAGE"));
            }

            return new ProcessedText(elements);
        }
    }
}
