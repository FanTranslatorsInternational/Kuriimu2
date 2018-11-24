using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kryptography.Nintendo
{
    internal static class NcaExtensions
    {
        internal static bool AnyInRange(this IEnumerable<SectionEntry> sections, long position)
        {
            foreach (var section in sections)
                if (position >= section.mediaOffset * Common.mediaSize && position <= section.endMediaOffset * Common.mediaSize)
                    return true;

            return false;
        }

        internal static int GetInRangeIndex(this List<SectionEntry> sections, long position)
        {
            for (int i = 0; i < sections.Count; i++)
                if (position >= sections[i].mediaOffset * Common.mediaSize && position <= sections[i].endMediaOffset * Common.mediaSize)
                    return i;

            return -1;
        }
    }
}
