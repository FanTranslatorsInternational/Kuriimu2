using Kontract.FileSystem2.Nodes.Abstract;

namespace Kontract.Interfaces.Common
{
    /// <summary>
    /// This interface allows a plugin to participate in automatic file identification.
    /// Plugins with this interface take priority over those without.
    /// </summary>
    public interface IIdentifyFiles
    {
        /// <summary>
        /// Determines if the given file is supported by the plugin.
        /// </summary>
        /// <param name="file">The file to be identified.</param>
        /// <param name="fileSystem">A file system object for the folder the input file was opened from.</param>
        /// <returns>True or False</returns>
        bool Identify(StreamInfo file,BaseReadOnlyDirectoryNode fileSystem);
    }
}
