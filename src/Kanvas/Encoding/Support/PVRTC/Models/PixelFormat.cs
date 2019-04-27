namespace Kanvas.Encoding.Support.PVRTC.Models
{
    public enum PixelFormat : ulong
    {
        PVRTCI_2bpp_RGB,
        PVRTCI_2bpp_RGBA,
        PVRTCI_4bpp_RGB,
        PVRTCI_4bpp_RGBA,
        PVRTCII_2bpp,
        PVRTCII_4bpp,
        ETC1,
        DXT1,
        DXT2,
        DXT3,
        DXT4,
        DXT5,

        //These formats are identical to some DXT formats.
        BC1 = DXT1,
        BC2 = DXT3,
        BC3 = DXT5,

        //These are currently unsupported:
        BC4,
        BC5,
        BC6,
        BC7,

        //These are supported
        UYVY,
        YUY2,
        BW1bpp,
        SharedExponentR9G9B9E5,
        RGBG8888,
        GRGB8888,
        ETC2_RGB,
        ETC2_RGBA,
        ETC2_RGB_A1,
        EAC_R11,
        EAC_RG11,

        RGB565 = 0x5060561626772,
        RGBA4444 = 0x404040461626772,
        RGBA8888 = 0x808080861626772,
    }
}
