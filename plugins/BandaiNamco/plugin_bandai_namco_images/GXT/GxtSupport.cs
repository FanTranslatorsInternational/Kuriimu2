using Komponent.IO.Attributes;

namespace plugin_bandai_namco_images.GXT
{
    /// <summary>
    /// 
    /// </summary>
    public class FileHeader
    {
        [FixedLength(4)]
        public string Magic = "GXT\0";
        public int Unk1;
        public int Unk2;
        public int Unk3;
        public int DataSize;
        public int Unk4;
        public int Unk5;
        public int Unk6;
        public int Unk7;
        public int ImageDataSize;
        public int Unk8;
        public int Unk9;
        public int Unk10;
        public int Unk11;
        public short Width;
        public short Height;
        public int Unk12;
    }

    /// <summary>
    /// 
    /// </summary>
    public enum ImageFormat : short
    {
        RGBA8888 = 0x00,
        Palette_8 = 0x05
    }
}
