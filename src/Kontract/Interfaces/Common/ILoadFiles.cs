using Kontract.Interfaces.VirtualFS;
using System;
using System.IO;

namespace Kontract.Interfaces.Common
{
    /// <inheritdoc />
    /// <summary>
    /// This interface allows a plugin to load files.
    /// </summary>
    public interface ILoadFiles : IDisposable
    {
        bool LeaveOpen { get; set; }

        /// <summary>
        /// Loads the given file.
        /// </summary>
        /// <param name="file">The file to be loaded.</param>
        void Load(StreamInfo file);
    }

    public class StreamInfo
    {
        public Stream FileData { get; set; }
        public string FileName { get; set; }
    }
}
