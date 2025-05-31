using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;

namespace plugin_atlus.N3DS.Archive
{
    struct HpiHeader
    {
        public string magic;
        public int zero0;
        public int headerSize;  //without magic and zero0
        public int zero1;
        public short zero2;
        public short hashCount;
        public int entryCount;
    }

    struct HpiHashEntry
    {
        public short entryOffset;
        public short entryCount;
    }

    struct HpiFileEntry
    {
        public int stringOffset;
        public int offset;
        public int compSize;
        public int decompSize;
    }

    class HpiHpbSupport
    {
        public static string PeekString(Stream input, int length)
        {
            var bkPos = input.Position;

            using var br = new BinaryReaderX(input, true);
            var result = br.ReadString(length);

            input.Position = bkPos;

            return result;
        }
    }

    class SlashFirstStringComparer : IComparer<UPath>
    {
        private static readonly IComparer<UPath> DefaultComparer = Comparer<UPath>.Default;

        private readonly IComparer<UPath> _comparer;

        public SlashFirstStringComparer() : this(DefaultComparer)
        {
        }

        public SlashFirstStringComparer(IComparer<UPath> stringComparer)
        {
            _comparer = stringComparer;
        }

        public int Compare(UPath x, UPath y)
        {
            if (x == y)
                return 0;

            var xFull = x.FullName;
            var yFull = y.FullName;

            // Find first difference
            var index = -1;
            for (var i = 0; i < Math.Min(xFull.Length, yFull.Length); i++)
                if (xFull[i] != yFull[i])
                {
                    index = i;
                    break;
                }

            // If no difference was found, use default comparer, instead of returning 0
            // This blocks false equality based on the default string comparer desired
            if (index == -1)
                return _comparer.Compare(x, y);

            if (xFull[index] == '.' && yFull[index] == '/')
                return 1;
            if (xFull[index] == '/' && yFull[index] == '.')
                return -1;

            return _comparer.Compare(x, y);
        }
    }
}
