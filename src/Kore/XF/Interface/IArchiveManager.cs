using System;
using System.Collections.Generic;
using System.IO;

namespace Kore.XFont.Interface
{
    public interface IArchiveManager : IFilePlugin
    {
        // Feature Support
        bool CanAddFiles { get; } // Is the plugin able to add files?
        bool CanRenameFiles { get; } // Is the plugin able to rename files?
        bool CanReplaceFiles { get; } // Is the plugin able to replace files?
        bool CanDeleteFiles { get; } // Is the plugin able to delete files?

        // I/O
        void Load(string filename);
        void Save(string filename = ""); // A non-blank filename is provided when using Save As...
        void Unload(); // Instructs the archive manager to close open file handles.

        // Files
        IEnumerable<ArchiveFileInfo> Files { get; } // File list.
        bool AddFile(ArchiveFileInfo afi);
        bool DeleteFile(ArchiveFileInfo afi);
    }

    public class ArchiveFileInfo
    {
        protected Stream _fileData = null;

        public string FileName { get; set; } = string.Empty; // Complete filename including path and extension.
        public virtual Stream FileData // Provides a stream to read the file data from.
        {
            get
            {
                _fileData.Position = 0;
                return _fileData;
            }
            set => _fileData = value;
        }
        public virtual long? FileSize => FileData?.Length; // The length of the (uncompressed) stream, override this in derived classes when FileData is also overridden.
        public ArchiveFileState State { get; set; } = ArchiveFileState.Empty; // Dictates the state of the ArchiveFileInfo to the UI. Plugins should not rely on this field for code logic.
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