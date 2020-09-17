using System.Collections.Generic;
using System.IO;
using System.Linq;
using DiscUtils;
using DiscUtils.Iso9660;
using Kontract.Models.Archive;

namespace plugin_sony.Archives
{
    class Ps2Disc
    {
        public IList<IArchiveFileInfo> Load(Stream input)
        {
            using var reader = new CDReader(input, false);
            return EnumerateFiles(reader.Root).ToArray();
        }

        private IEnumerable<IArchiveFileInfo> EnumerateFiles(DiscDirectoryInfo directoryInfo)
        {
            foreach (var file in directoryInfo.GetFiles())
                yield return new Ps2DiscArchiveFileInfo(file);

            foreach (var directory in directoryInfo.GetDirectories())
                foreach (var file in EnumerateFiles(directory))
                    yield return file;
        }
    }
}
