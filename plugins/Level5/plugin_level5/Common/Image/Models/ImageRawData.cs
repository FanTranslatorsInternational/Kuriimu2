namespace plugin_level5.Common.Image.Models
{
    public class ImageRawData
    {
        public FormatVersion Version { get; set; }

        public int BitDepth { get; set; }
        public int Format { get; set; }

        public int PaletteBitDepth { get; set; } = -1;
        public int PaletteFormat { get; set; } = -1;

        public int Width { get; set; }
        public int Height { get; set; }

        public byte[]? LegacyData { get; set; }

        public byte[] Data { get; set; }
        public byte[][] MipMapData { get; set; }
        public byte[]? PaletteData { get; set; }
    }
}
