﻿using System.Threading.Tasks;
using Kontract.Interfaces.FileSystem;
using Kontract.Models.FileSystem;
using Kontract.Models.Plugins.Entry;

namespace Kontract.Interfaces.Plugins.Entry
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
        /// <param name="identifyContext">The context for this identify operation, containing environment instances.</param>
        /// <returns>If the file is supported by this plugin.</returns>
        Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext);
    }
}
