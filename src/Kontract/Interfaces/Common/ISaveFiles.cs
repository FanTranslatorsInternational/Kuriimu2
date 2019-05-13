using Kontract.FileSystem2.Nodes.Physical;

namespace Kontract.Interfaces.Common
{
    /// <summary>
    /// This interface allows a plugin to save files.
    /// </summary>
    public interface ISaveFiles
    {
        /// <summary>
        /// Allows a plugin to save files.
        /// </summary>
        /// <param name="output">The file to be saved.</param>
        /// <param name="fileSystem">A file system object of temporary folder, to write all files into.</param>
        /// <param name="versionIndex">The version index that the user selected.</param>
        void Save(StreamInfo output, PhysicalDirectoryNode fileSystem, int versionIndex = 0);
    }
}
