using System;
using System.IO;
using System.Threading.Tasks;
using Kontract.Extensions;
using Kontract.Interfaces;
using Kontract.Interfaces.Providers;

namespace Kontract.Models.Archive
{
    /// <summary>
    /// The base model to represent a loaded file in an archive state.
    /// </summary>
    public class ArchiveFileInfo
    {
        private UPath _filePath;

        /// <summary>
        /// Determines of the content of this file info was modified.
        /// </summary>
        public bool ContentChanged { get; set; }

        /// <summary>
        /// Retrieve a list of plugins, this file can be opened with.
        /// Any other plugins won't be allowed to pass this file on.
        /// </summary>
        public Guid[] PluginIds { get; set; }

        /// <summary>
        /// The data stream for this file info.
        /// </summary>
        protected Stream FileData { get; set; }

        /// <summary>
        /// The path of the file info into the archive.
        /// </summary>
        public UPath FilePath
        {
            get => _filePath;
            set => _filePath = value.ToAbsolute();
        }

        /// <summary>
        /// The size of the file data.
        /// </summary>
        public virtual long FileSize => FileData?.Length ?? 0;

        /// <summary>
        /// Creates a new instance of <see cref="ArchiveFileInfo"/>.
        /// </summary>
        /// <param name="fileData">The data stream for this file info.</param>
        /// <param name="filePath">The path of the file into the archive.</param>
        public ArchiveFileInfo(Stream fileData, string filePath)
        {
            ContractAssertions.IsNotNull(fileData, nameof(fileData));
            ContractAssertions.IsNotNull(filePath, nameof(filePath));

            FileData = fileData;
            FilePath = filePath;
        }

        /// <summary>
        /// Gets the file data from this file info.
        /// </summary>
        /// <param name="temporaryStreamProvider">A provider for temporary streams.</param>
        /// <param name="progress">The context to report progress to.</param>
        /// <returns>The file data for this file info.</returns>
        /// <remarks>The <see cref="ITemporaryStreamProvider"/> is used for decrypting or decompressing files temporarily onto the disk to minimize memory usage.</remarks>
        public virtual Task<Stream> GetFileData(ITemporaryStreamProvider temporaryStreamProvider, IKuriimuProgress progress = null)
        {
            return Task.FromResult(FileData);
        }

        /// <summary>
        /// Sets the file data for this file info.
        /// </summary>
        /// <param name="newFileData">The new file data for this file info.</param>
        /// <remarks>This method should only set the file data, without compressing or encrypting the data yet.</remarks>
        public virtual void SetFileData(Stream newFileData)
        {
            if (FileData == newFileData)
                return;

            ContentChanged = true;

            FileData.Close();
            FileData = newFileData;
        }

        /// <summary>
        /// Save the file data to an output stream.
        /// </summary>
        /// <param name="output">The output to write the file data to.</param>
        /// <param name="progress">The context to report progress to.</param>
        public virtual void SaveFileData(Stream output, IKuriimuProgress progress)
        {
            var bkPos = FileData.Position;
            FileData.Position = 0;

            progress?.Report($"Writing file '{FilePath}'.", 0.0);

            // TODO: Change that to a manual bulk copy to better watch progress?
            FileData.CopyTo(output);

            progress?.Report($"Writing file '{FilePath}'.", 100.0);

            FileData.Position = bkPos;

            ContentChanged = false;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return FilePath.FullName;
        }
    }
}
