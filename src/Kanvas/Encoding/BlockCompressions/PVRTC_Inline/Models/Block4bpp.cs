using Komponent.IO.Attributes;

namespace Kanvas.Encoding.BlockCompressions.PVRTC_Inline.Models
{
    // http://cdn.imgtec.com/sdk-documentation/PVRTC Specification and User Guide.pdf
    [BitFieldInfo(BlockSize = 8)]
    class Block4bpp
    {
        public ColorB colorB;
        public ColorA colorA;
        [BitField(1)] public bool modulationFlag;
        [FixedLength(16)] public ModulationEntry[] modulationEntries;
    }

    class ColorB
    {
        [BitField(1)] public bool opacityFlag;

        [TypeChoice("opacityFlag", TypeChoiceComparer.Equal, 1, typeof(ColorBOpaque))]
        [TypeChoice("opacityFlag", TypeChoiceComparer.Equal, 0, typeof(ColorBTranslucent))]
        public object components;
    }

    class ColorBOpaque
    {
        [BitField(5)] public int red;
        [BitField(5)] public int green;
        [BitField(5)] public int blue;
    }

    class ColorBTranslucent
    {
        [BitField(3)] public int alpha;
        [BitField(4)] public int red;
        [BitField(4)] public int green;
        [BitField(4)] public int blue;
    }

    class ColorA
    {
        [BitField(1)] public bool opacityFlag;

        [TypeChoice("opacityFlag", TypeChoiceComparer.Equal, 1, typeof(ColorAOpaque))]
        [TypeChoice("opacityFlag", TypeChoiceComparer.Equal, 0, typeof(ColorATranslucent))]
        public object components;
    }

    class ColorAOpaque
    {
        [BitField(5)] public int red;
        [BitField(5)] public int green;
        [BitField(4)] public int blue;
    }

    class ColorATranslucent
    {
        [BitField(3)] public int alpha;
        [BitField(4)] public int red;
        [BitField(4)] public int green;
        [BitField(3)] public int blue;
    }

    class ModulationEntry
    {
        [BitField(2)] public byte modulationData;
    }
}
