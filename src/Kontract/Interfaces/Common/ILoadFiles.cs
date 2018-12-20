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
        /// <summary>
        /// Gives the minimum required file count for this format
        /// </summary>
        int MinimumRequiredFiles { get; }

        /// <summary>
        /// Loads the given file.
        /// </summary>
        /// <param name="filename">The file to be loaded.</param>
        void Load(params StreamInfo[] filename);
    }

    public class StreamInfo
    {
        public Stream FileData { get; set; }
        public string FileName { get; set; }
    }
}
