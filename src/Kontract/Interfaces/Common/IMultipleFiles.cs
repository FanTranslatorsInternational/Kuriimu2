using Kontract.Interfaces.FileSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kontract.Interfaces.Common
{
    public interface IMultipleFiles
    {
        IFileSystem FileSystem { get; set; }
    }

    /// <summary>
    /// Allows the event handler to open files with a matching pattern automatically
    /// </summary>
    public class RequestFileEventArgs : EventArgs
    {
        /// <summary>
        /// FilePathPattern allows for any Regular Expression to open files matching it, based on the directory of the initial file
        /// </summary>
        public string FilePathPattern;

        /// <summary>
        /// All StreamInfos opened based on the given pattern
        /// </summary>
        public StreamInfo[] OpenedStreamInfos = null;
    }
}
