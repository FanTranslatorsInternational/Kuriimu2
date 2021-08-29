using Komponent.IO.Attributes;

namespace plugin_mercury_steam.Images
{
    class MtxtHeader
    {
        [FixedLength(4)]
        public string magic;

        public int unk1;
        public int unk2;

        public int width;
        public int height;
        public int format;

        public int nameOffset;
        public int offset;
        public int imgSize;
    }
}
