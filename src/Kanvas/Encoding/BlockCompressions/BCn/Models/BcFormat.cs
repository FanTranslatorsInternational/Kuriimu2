namespace Kanvas.Encoding.BlockCompressions.BCn.Models
{
    public enum BcFormat
    {
        BC1,
        BC2,
        BC3,
        BC4,
        BC5,

        DXT1 = BC1,
        DXT2 = BC2,
        DXT3 = BC2,
        DXT4 = BC3,
        DXT5 = BC3,

        ATI1 = BC4,
        ATI2 = BC5,

        ATI1A_WiiU,
        ATI1L_WiiU,
        ATI2_WiiU
    }
}
