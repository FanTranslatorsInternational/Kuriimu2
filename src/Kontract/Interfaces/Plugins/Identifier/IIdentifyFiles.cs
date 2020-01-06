using System.Threading.Tasks;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Providers;
using Kontract.Models;
using Kontract.Models.IO;

namespace Kontract.Interfaces.Plugins.Identifier
{
    /// <summary>
    /// Exposes methods to identify if a file is supported.
    /// </summary>
    public interface IIdentifyFiles
    {
        /// <summary>
        /// Identify if a file is supported by this plugin.
        /// </summary>
        /// <param name="fileSystem">The file system from which the file is requested.</param>
        /// <param name="filePath">The path to the file requested by the user.</param>
        /// <param name="temporaryStreamProvider">A provider for temporary streams.</param>
        /// <returns>If the file is supported by this plugin.</returns>
        Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, ITemporaryStreamProvider temporaryStreamProvider);
    }
}
