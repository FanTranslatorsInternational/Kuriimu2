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
        public KoreSaveInfo(KoreFileInfo kfi, string tempFolder)
        {
            Kfi = kfi;
            TempFolder = tempFolder;
        }

        public KoreFileInfo Kfi { get; }
        public int Version { get; set; }
        public string NewSaveLocation { get; set; }
        public string TempFolder { get; }
    }
}
