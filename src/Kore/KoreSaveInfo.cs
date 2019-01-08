using Kontract.Interfaces.Archive;
using Kontract.Interfaces.FileSystem;
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
        public string TempFolder { get; }
        public string NewSaveLocation { get; set; }
        public int Version { get; set; }

        public KoreFileInfo SavedKfi { get; set; }
    }
}
