using System;
using System.Text;
using Komponent.IO.Attributes;
using Kontract.Models.Text;
using Kontract.Models.Text.ControlCodeProcessor;

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
        public int labelOffset;
    }

    class GmdEntryV2
    {
        public int sectionId;
        public uint hash1;
        public uint hash2;
        public int labelOffset;
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

    class GmdControlCodeProcessor : IControlCodeProcessor
    {
        public ProcessedText Read(byte[] data, Encoding encoding)
        {
            return new ProcessedText(string.Empty);
        }

        public byte[] Write(ProcessedText text, Encoding encoding)
        {
            return Array.Empty<byte>();
        }
    }
}
