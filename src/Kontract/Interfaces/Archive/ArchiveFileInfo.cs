using System;
using System.Diagnostics;
using System.IO;

namespace Kontract.Interfaces.Archive
{
    /// <summary>
    /// the base class for a file in an archive to load in Kuriimu2
    /// </summary>
    [DebuggerDisplay("{FileName}")]
    public class ArchiveFileInfo
    {
        /// <summary>
        /// Internal file data
        /// </summary>
        protected Stream _fileData;

        /// <summary>
        /// The complete name of the file including path and extension.
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// Provides a stream to read the file data from.
        /// </summary>
        public virtual Stream FileData // Provides a stream to read the file data from.
        {
            get
            {
                if (_fileData != null)
                    _fileData.Position = 0;
                return _fileData;
            }
            set => _fileData = value;
        }

        /// <summary>
        /// The length of the (uncompressed) stream
        /// </summary>
        /// <remarks>Override in derived classes when FileData gets overridden</remarks>
        public virtual long? FileSize => FileData?.Length;

        /// <summary>
        /// Dictates the state of the ArchiveFileInfo
        /// </summary>
        /// <remarks>Plugins should not rely on this property for code logic</remarks>
        public ArchiveFileState State { get; set; } = ArchiveFileState.Empty;

        /// <summary>
        /// Holds all plugin FQNs that could potentially open that file; Other plugins get ignored for opening it
        /// </summary>
        /// <remarks>If null or empty, this property gets ignored.</remarks>
        public string[] PluginNames { get; set; }
    }

    [Flags]
    public enum ArchiveFileState
    {
        Empty = 0,
        Archived = 1,
        Added = 2,
        Replaced = 4,
        Renamed = 8,
        Deleted = 16
    }
}
