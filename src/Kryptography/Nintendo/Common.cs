namespace Kryptography.Nintendo
{
    internal class Common
    {
        internal const long mediaSize = 0x200;
        internal const long ncaHeaderSize = 0xC00;
    }

    public class SectionEntry
    {
        public int mediaOffset;
        public int endMediaOffset;
        public int unk1;
        public int unk2;
    }

    public enum NCAVersion : int
    {
        None = 0,
        NCA2 = 2,
        NCA3 = 3
    }
}