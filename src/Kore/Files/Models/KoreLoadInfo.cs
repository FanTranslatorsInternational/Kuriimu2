using System.IO;
using Kontract.FileSystem.Nodes.Abstract;
using Kontract.Interfaces.Common;

namespace Kore.Files.Models
{
    // TODO: Documentation and proper creation of the object
    public class KoreLoadInfo
    {
        public string FileName { get; }
        public Stream FileData { get; }

        public ILoadFiles Adapter { get; set; }

        public BaseReadOnlyDirectoryNode FileSystem { get; set; }
        public bool LeaveOpen { get; set; }
        public bool TrackFile { get; set; } = true;

        public KoreLoadInfo(Stream stream, string filename)
        {
            FileName = filename;
            FileData = stream;
        }
    }
}
