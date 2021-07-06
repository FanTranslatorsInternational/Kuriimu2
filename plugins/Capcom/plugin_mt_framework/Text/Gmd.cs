using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Models.IO;
using Kontract.Models.Text;

namespace plugin_mt_framework.Text
{
    class Gmd
    {
        private static readonly int HeaderSize = Tools.MeasureType(typeof(GmdHeader));
        private static readonly int EntrySizeV1 = Tools.MeasureType(typeof(GmdEntryV1));
        private static readonly int EntrySizeV2 = Tools.MeasureType(typeof(GmdEntryV2));
        private static readonly int EntrySizeV2Mobile = Tools.MeasureType(typeof(GmdEntryV2Mobile));

        private ByteOrder _byteOrder;
        private GmdHeader _header;
        private string _name;
        private int _keyIndex;

        public IList<TextEntry> Load(Stream input)
        {
            using var br = new BinaryReaderX(input);

            // Determine byte order
            _byteOrder = br.ByteOrder = br.ReadString(4) == "GMD\0" ? ByteOrder.LittleEndian : ByteOrder.BigEndian;
            br.BaseStream.Position = 0;

            // Read header
            _header = br.ReadType<GmdHeader>();
            _name = br.ReadString(_header.nameSize, Encoding.ASCII);

            br.SeekAlignment(4);

            // Determine reading method
            switch (_header.version)
            {
                // Version 1
                case 0x00010201:
                    return ReadVersion1(br);

                // Version 2
                case 0x00010302:
                    return ReadVersion2(br);

                default:
                    throw new InvalidOperationException($"Unsupported version {_header.version:X8}.");
            }
        }

        #region Load

        private IList<TextEntry> ReadVersion1(BinaryReaderX br)
        {
            // Read entry infos
            var entries = br.ReadMultiple<GmdEntryV1>(_header.sectionCount);

            // Skip labels
            var labelOffset = br.BaseStream.Position;
            br.BaseStream.Position += _header.labelSize;

            // Wrap section data into decryption, if necessary
            Stream sectionStream = new SubStream(br.BaseStream, br.BaseStream.Position, _header.sectionSize);
            if (GmdSupport.IsEncrypted(sectionStream))
            {
                _keyIndex = GmdSupport.DetermineKeyIndex(sectionStream);
                sectionStream = GmdSupport.WrapXor(sectionStream, _keyIndex);
            }

            // Read section data
            using var sectionBr = new BinaryReaderX(sectionStream);

            var result = new List<TextEntry>();
            for (var i = 0; i < _header.labelCount; i++)
            {
                var sectionData = sectionBr.ReadBytesUntil(0);
                var label = string.Empty;

                // Get label
                var entryInfo = entries.FirstOrDefault(x => x.sectionId == i);
                if (entryInfo != default)
                {
                    var bkPos = br.BaseStream.Position;
                    br.BaseStream.Position = labelOffset + entryInfo.labelOffset - 0x290802F0;
                    label = br.ReadCStringASCII();
                    br.BaseStream.Position = bkPos;
                }

                // Create entry
                var entry = string.IsNullOrEmpty(label) ? new TextEntry() : new TextEntry(label);
                entry.Encoding = Encoding.UTF8;
                entry.TextData = sectionData;
                entry.ControlCodeProcessor = new GmdControlCodeProcessor();
                entry.TextPager = new GmdTextPager();

                result.Add(entry);
            }

            return result;
        }

        private IList<TextEntry> ReadVersion2(BinaryReaderX br)
        {
            // Determine mobile entries
            var mobileLength = HeaderSize + _header.labelSize + _header.sectionSize +
                               0x400 + ((_header.nameSize + 3) & ~3) + _header.labelCount * EntrySizeV2Mobile;

            // Read entry info
            (int, long)[] entryInfos;
            if (br.BaseStream.Length == mobileLength)
            {
                entryInfos = br.ReadMultiple<GmdEntryV2Mobile>(_header.sectionCount)
                    .Select(e => (e.sectionId, e.labelOffset))
                    .ToArray();
            }
            else
            {
                entryInfos = br.ReadMultiple<GmdEntryV2>(_header.sectionCount)
                    .Select(e => (e.sectionId, (long)e.labelOffset))
                    .ToArray();
            }

            // Skip bucket list
            br.BaseStream.Position += 0x400;

            // Skip labels
            var labelOffset = br.BaseStream.Position;
            br.BaseStream.Position += _header.labelSize;

            // Wrap section data into decryption, if necessary
            Stream sectionStream = new SubStream(br.BaseStream, br.BaseStream.Position, _header.sectionSize);
            if (GmdSupport.IsEncrypted(sectionStream))
            {
                _keyIndex = GmdSupport.DetermineKeyIndex(sectionStream);
                sectionStream = GmdSupport.WrapXor(sectionStream, _keyIndex);
            }

            // Read section data
            using var sectionBr = new BinaryReaderX(sectionStream);

            var result = new List<TextEntry>();
            for (var i = 0; i < _header.labelCount; i++)
            {
                var sectionData = sectionBr.ReadBytesUntil(0);
                var label = string.Empty;

                // Get label
                var entryInfo = entryInfos.FirstOrDefault(x => x.Item1 == i);
                if (entryInfo != default)
                {
                    var bkPos = br.BaseStream.Position;
                    br.BaseStream.Position = labelOffset + entryInfo.Item2;
                    label = br.ReadCStringASCII();
                    br.BaseStream.Position = bkPos;
                }

                // Create entry
                var entry = string.IsNullOrEmpty(label) ? new TextEntry() : new TextEntry(label);
                entry.Encoding = Encoding.UTF8;
                entry.TextData = sectionData;
                entry.ControlCodeProcessor = new GmdControlCodeProcessor();
                entry.TextPager = new GmdTextPager();

                result.Add(entry);
            }

            return result;
        }

        #endregion
    }
}
