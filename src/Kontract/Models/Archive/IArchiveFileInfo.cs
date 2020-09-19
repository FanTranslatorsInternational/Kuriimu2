using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kontract.Interfaces.Progress;
using Kontract.Interfaces.Providers;
using Kontract.Models.IO;

namespace Kontract.Models.Archive
{
    public interface IArchiveFileInfo
    {
        /// <summary>
        /// Determines if the FileData is compressed, and has to be handled as such
        /// </summary>
        bool UsesCompression { get; }

        /// <summary>
        /// Determines if the content of this file info was modified.
        /// </summary>
        bool ContentChanged { get; set; }

        /// <summary>
        /// Retrieve a list of plugins, this file can be opened with.
        /// Any other plugins won't be allowed to pass this file on.
        /// </summary>
        Guid[] PluginIds { get; set; }

        /// <summary>
        /// The path of the file info into the archive.
        /// </summary>
        UPath FilePath { get; set; }

        /// <summary>
        /// The size of the file data.
        /// </summary>
        long FileSize { get; }

        /// <summary>
        /// Gets the file data from this file info.
        /// </summary>
        /// <param name="temporaryStreamProvider">A provider for temporary streams.</param>
        /// <param name="progress">The context to report progress to.</param>
        /// <returns>The file data for this file info.</returns>
        /// <remarks>The <see cref="ITemporaryStreamProvider"/> is used for decrypting or decompressing files temporarily onto the disk to minimize memory usage.</remarks>
        Task<Stream> GetFileData(ITemporaryStreamProvider temporaryStreamProvider = null, IProgressContext progress = null);

        /// <summary>
        /// Sets the file data for this file info.
        /// </summary>
        /// <param name="fileData">The new file data for this file info.</param>
        /// <remarks>This method should only set the file data, without compressing or encrypting the data yet.</remarks>
        void SetFileData(Stream fileData);
    }
}
