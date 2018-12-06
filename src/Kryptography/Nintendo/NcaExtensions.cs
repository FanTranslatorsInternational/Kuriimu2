using System.Collections.Generic;

namespace Kryptography.Nintendo
{
    internal static class NcaExtensions
    {
        internal static bool AnyInRange(this IEnumerable<SectionEntry> sections, long position)
        {
            foreach (var section in sections)
                if (position >= section.mediaOffset * Common.MediaSize && position <= section.endMediaOffset * Common.MediaSize)
                    return true;

            return false;
        }

        internal static int GetInRangeIndex(this List<SectionEntry> sections, long position)
        {
            for (int i = 0; i < sections.Count; i++)
                if (position >= sections[i].mediaOffset * Common.MediaSize && position <= sections[i].endMediaOffset * Common.MediaSize)
                    return i;

            return -1;
        }
    }
}