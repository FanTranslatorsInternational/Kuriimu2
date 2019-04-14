using System.Collections.Generic;
using Kontract.Interfaces.Common;

namespace Kontract.Interfaces.Archive
{
    /// <summary>
    /// 
    /// </summary>
    public interface IArchiveAdapter : IPlugin
    {
        /// <summary>
        /// 
        /// </summary>
        List<ArchiveFileInfo> Files { get; }
        
        /// <summary>
        /// 
        /// </summary>
        bool FileHasExtendedProperties { get; }
    }
}
