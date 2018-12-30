using Kontract.Interfaces.Archive;
using Kontract.Interfaces.VirtualFS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kore
{
    public class KoreSaveInfo
    {
        public IArchiveAdapter ParentAdapter { get; set; }
        public string TempFolder { get; set; }

        public string NewSaveLocation { get; set; }

        public int Version { get; set; }
    }
}
