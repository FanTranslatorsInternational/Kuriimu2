using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kontract.Interfaces.Archive
{
    public interface IArchiveRenameFiles
    {
        void RenameFile(ArchiveFileInfo afi, string newFilename);
    }
}
