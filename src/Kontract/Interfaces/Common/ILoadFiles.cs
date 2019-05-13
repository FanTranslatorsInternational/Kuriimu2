using System;
using System.IO;
using Kontract.FileSystem2.Nodes.Abstract;

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
        /// Loads the given file stream.
        /// </summary>
        /// <param name="input">The file stream to be loaded.</param>
        /// <param name="fileSystem">A file system object for the folder the input file was opened from.</param>
        /// <remarks><paramref name="fileSystem"/> is read-only.</remarks>
        void Load(StreamInfo input,BaseReadOnlyDirectoryNode fileSystem);
    }

    /// <summary>
    /// A data class that represents stream information used for loading and saving files.
    /// </summary>
    public class StreamInfo
    {
        /// <summary>
        /// The underlying data stream to read from and write to.
        /// </summary>
        public Stream FileData { get; set; }

        /// <summary>
        /// The name of the file associated with the strean.
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Creates a new blank <see cref="StreamInfo"/> object.
        /// </summary>
        public StreamInfo() { }

        /// <summary>
        /// Creates a new populated <see cref="StreamInfo"/> object.
        /// </summary>
        /// <param name="fileData">A readable or writeable stream for the underlying file data.</param>
        /// <param name="fileName">The name or intended name of the associated file.</param>
        public StreamInfo(Stream fileData, string fileName)
        {
            FileData = fileData;
            FileName = fileName;
        }
    }
}
