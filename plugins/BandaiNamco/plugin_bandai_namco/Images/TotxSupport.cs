using Komponent.IO.Attributes;

namespace plugin_bandai_namco.Images
{
    class TotxHeader
    {
        [FixedLength(4)] 
        public string magic = "TOTX";
        public int zero0;
        public int zero1;
        public short width;
        public short height;
    }

    class TotxSupport
    {
    }
}
