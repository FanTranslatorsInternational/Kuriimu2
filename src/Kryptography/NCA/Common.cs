namespace Kryptography.NCA
{
    internal class Common
    {
        public const long mediaSize = 0x200;
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
        NCA2,
        NCA3
    }
}
