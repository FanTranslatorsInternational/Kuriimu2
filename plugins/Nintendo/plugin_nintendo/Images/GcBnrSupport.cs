using Komponent.IO.Attributes;

namespace plugin_nintendo.Images
{
    [Alignment(0x20)]
    class GcBnrHeader
    {
        [FixedLength(4)]
        public string magic;    // BNR1 or BNR2
    }

    class GcBnrTitleInfo
    {
        [FixedLength(0x20, StringEncoding = StringEncoding.SJIS)]
        public string gameName;
        [FixedLength(0x20, StringEncoding = StringEncoding.SJIS)]
        public string company;
        [FixedLength(0x40, StringEncoding = StringEncoding.SJIS)]
        public string fullGameName;
        [FixedLength(0x40, StringEncoding = StringEncoding.SJIS)]
        public string fullCompany;
        [FixedLength(0x80, StringEncoding = StringEncoding.SJIS)]
        public string description;
    }
}
