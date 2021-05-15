using Komponent.IO.Attributes;
using Komponent.IO.BinarySupport;

namespace plugin_nintendo.NW4R
{
    public class Nw4rCommonHeader
    {
        [FixedLength(4)]
        public string magic;
        public int size;
        public int version;
        public int bresOffset;
        [CalculateLength(typeof(CommonHeaderSupport), nameof(CommonHeaderSupport.GetSectionCount))]
        public int[] sectionOffsets;
        public int nameOffset;
    }

    static class CommonHeaderSupport
    {
        public static int GetSectionCount(ValueStorage storage)
        {
            var magic = (string)storage.Get("magic");
            var version = (int)storage.Get("version");

            switch (magic)
            {
                case "TEX0":
                    switch (version)
                    {
                        case 2:
                            return 2;

                        default:
                            return 1;
                    }

                case "PLT0":
                    return 1;

                default:
                    return 0;
            }
        }
    }
}
