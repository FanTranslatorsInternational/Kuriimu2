using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kontract.Interfaces.Archive
{
    public interface IArchiveDeleteFile
    {
        void DeleteFile(ArchiveFileInfo afi);
    }
}
