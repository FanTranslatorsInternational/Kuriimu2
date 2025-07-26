using Kanvas.Contract.Encoding;
using Kanvas.Encoding;
using Komponent.Contract.Enums;
using Index = Kanvas.Encoding.Index;

namespace Kanvas
{
    public static class ImageFormats
    {
        public static IColorEncoding Rgba1010102(ByteOrder byteOrder = ByteOrder.LittleEndian) => new Rgba(10, 10, 10, 2, byteOrder);
        public static IColorEncoding Rgba8888(ByteOrder byteOrder = ByteOrder.LittleEndian) => new Rgba(8, 8, 8, 8, byteOrder);
        public static IColorEncoding Rgb888() => new Rgba(8, 8, 8);
        public static IColorEncoding Bgr888() => new Rgba(8, 8, 8, "BGR");
        public static IColorEncoding Rgba5551(ByteOrder byteOrder = ByteOrder.LittleEndian) => new Rgba(5, 5, 5, 1, byteOrder);
        public static IColorEncoding Rgb565(ByteOrder byteOrder = ByteOrder.LittleEndian) => new Rgba(5, 6, 5, byteOrder);
        public static IColorEncoding Rgb555(ByteOrder byteOrder = ByteOrder.LittleEndian) => new Rgba(5, 5, 5, byteOrder);
        public static IColorEncoding Rgba4444(ByteOrder byteOrder = ByteOrder.LittleEndian) => new Rgba(4, 4, 4, 4, byteOrder);
        public static IColorEncoding Rg88(ByteOrder byteOrder = ByteOrder.LittleEndian) => new Rgba(8, 8, 0, byteOrder);

        public static IColorEncoding L8() => new La(8, 0);
        public static IColorEncoding L4(BitOrder bitOrder = BitOrder.MostSignificantBitFirst) => new La(4, 0, ByteOrder.LittleEndian, bitOrder);
        public static IColorEncoding A8() => new La(0, 8);
        public static IColorEncoding A4(BitOrder bitOrder = BitOrder.MostSignificantBitFirst) => new La(0, 4, ByteOrder.LittleEndian, bitOrder);
        public static IColorEncoding La88(ByteOrder byteOrder = ByteOrder.LittleEndian) => new La(8, 8, byteOrder);
        public static IColorEncoding La44() => new La(4, 4);
        public static IColorEncoding Al44() => new La(4, 4, "AL");

        public static IIndexEncoding I2(BitOrder bitOrder = BitOrder.MostSignificantBitFirst) => new Index(2, ByteOrder.LittleEndian, bitOrder);
        public static IIndexEncoding I4(BitOrder bitOrder = BitOrder.MostSignificantBitFirst) => new Index(4, ByteOrder.LittleEndian, bitOrder);
        public static IIndexEncoding I8() => new Index(8);
        public static IIndexEncoding Ia53() => new Index(5, 3);
        public static IIndexEncoding Ia35() => new Index(3, 5);

        public static IColorEncoding Etc1(bool zOrder, ByteOrder byteOrder = ByteOrder.LittleEndian) => new Etc1(false, zOrder, byteOrder);
        public static IColorEncoding Etc1A4(bool zOrder, ByteOrder byteOrder = ByteOrder.LittleEndian) => new Etc1(true, zOrder, byteOrder);

        public static IColorEncoding Etc2() => new Etc2(Etc2Format.ETC2_RGB);
        public static IColorEncoding Etc2A() => new Etc2(Etc2Format.ETC2_RGBA);
        public static IColorEncoding Etc2A1() => new Etc2(Etc2Format.ETC2_RGB_A1);
        public static IColorEncoding EacR11() => new Etc2(Etc2Format.EAC_R11);
        public static IColorEncoding EacRG11() => new Etc2(Etc2Format.EAC_RG11);

        public static IColorEncoding Dxt1() => new Bc(BcFormat.Dxt1);
        public static IColorEncoding Dxt3() => new Bc(BcFormat.Dxt3);
        public static IColorEncoding Dxt5() => new Bc(BcFormat.Dxt5);
        public static IColorEncoding Ati1() => new Bc(BcFormat.Ati1);
        public static IColorEncoding Ati2() => new Bc(BcFormat.Ati2);
        public static IColorEncoding Ati1L() => new Bc(BcFormat.Ati1L);
        public static IColorEncoding Ati1A() => new Bc(BcFormat.Ati1A);
        public static IColorEncoding Ati2AL() => new Bc(BcFormat.Ati2AL);

        public static IColorEncoding Atc() => new Atc(AtcFormat.Atc);
        public static IColorEncoding AtcExplicit() => new Atc(AtcFormat.Atc_Explicit);
        public static IColorEncoding AtcInterpolated() => new Atc(AtcFormat.Atc_Interpolated);

        public static IColorEncoding Bc6H() => new Bc(BcFormat.Bc6H);
        public static IColorEncoding Bc7() => new Bc(BcFormat.Bc7);

        public static IColorEncoding Pvrtc_2bpp() => new PVRTC(PvrtcFormat.PVRTCI_2bpp_RGB);
        public static IColorEncoding PvrtcA_2bpp() => new PVRTC(PvrtcFormat.PVRTCI_2bpp_RGBA);
        public static IColorEncoding Pvrtc_4bpp() => new PVRTC(PvrtcFormat.PVRTCI_4bpp_RGB);
        public static IColorEncoding PvrtcA_4bpp() => new PVRTC(PvrtcFormat.PVRTCI_4bpp_RGBA);
        public static IColorEncoding Pvrtc2_2bpp() => new PVRTC(PvrtcFormat.PVRTCII_2bpp);
        public static IColorEncoding Pvrtc2_4bpp() => new PVRTC(PvrtcFormat.PVRTCII_4bpp);

        public static IColorEncoding Astc4x4() => new Astc(AstcFormat.ASTC_4x4);
        public static IColorEncoding Astc5x4() => new Astc(AstcFormat.ASTC_5x4);
        public static IColorEncoding Astc5x5() => new Astc(AstcFormat.ASTC_5x5);
        public static IColorEncoding Astc6x5() => new Astc(AstcFormat.ASTC_6x5);
        public static IColorEncoding Astc6x6() => new Astc(AstcFormat.ASTC_6x6);
        public static IColorEncoding Astc8x5() => new Astc(AstcFormat.ASTC_8x5);
        public static IColorEncoding Astc8x6() => new Astc(AstcFormat.ASTC_8x6);
        public static IColorEncoding Astc8x8() => new Astc(AstcFormat.ASTC_8x8);
        public static IColorEncoding Astc10x5() => new Astc(AstcFormat.ASTC_10x5);
        public static IColorEncoding Astc10x6() => new Astc(AstcFormat.ASTC_10x6);
        public static IColorEncoding Astc10x8() => new Astc(AstcFormat.ASTC_10x8);
        public static IColorEncoding Astc10x10() => new Astc(AstcFormat.ASTC_10x10);
        public static IColorEncoding Astc12x10() => new Astc(AstcFormat.ASTC_12x10);
        public static IColorEncoding Astc12x12() => new Astc(AstcFormat.ASTC_12x12);
    }
}
