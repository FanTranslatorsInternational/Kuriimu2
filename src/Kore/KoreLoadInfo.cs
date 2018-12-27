using Kontract.Interfaces.Common;
using Kontract.Interfaces.VirtualFS;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kore
{
    public class KoreLoadInfo
    {
        public string FileName { get; set; }
        public Stream FileData { get; set; }

        public ILoadFiles Adapter { get; set; }

        public IVirtualFSRoot FileSystem { get; set; }
        public bool LeaveOpen { get; set; }
        public bool TrackFile { get; set; } = true;

        public KoreLoadInfo(Stream stream, string filename)
        {
            FileName = filename;
            FileData = stream;
        }
    }
}
