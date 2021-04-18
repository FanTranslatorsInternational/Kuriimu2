using System;
using System.Threading.Tasks;
using Kontract.Extensions;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models;
using Kontract.Models.Context;
using Kontract.Models.IO;

namespace Kontract.Interfaces.Plugins.Identifier
{
    /// <summary>
    /// Base interface for plugins that handle files.
    /// </summary>
    /// <see cref="PluginType"/> for the supported types of files.
    public interface IFilePlugin : IPlugin
    {
        /// <summary>
        /// The type of file the plugin can handle.
        /// </summary>
        PluginType PluginType { get; }
        
        /// <summary>
        /// All file extensions the format can be identified with.
        /// </summary>
        string[] FileExtensions { get; }
        
        #region Optional features

        /// <summary>
        /// Creates an <see cref="IPluginState"/> to further work with the file.
        /// </summary>
        /// <param name="pluginManager">The plugin manager to load files with the Kuriimu runtime.</param>
        /// <returns>Newly created <see cref="IPluginState"/>.</returns>
        IPluginState CreatePluginState(IPluginManager pluginManager);
        
        /// <summary>
        /// Identify if a file is supported by this plugin.
        /// </summary>
        /// <param name="fileSystem">The file system from which the file is requested.</param>
        /// <param name="filePath">The path to the file requested by the user.</param>
        /// <param name="identifyContext">The context for this identify operation, containing environment instances.</param>
        /// <returns>If the file is supported by this plugin.</returns>
        public Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            throw new InvalidOperationException();
        }
        
        #endregion
        
        #region Optional feature support checks
        
        public bool CanIdentifyFiles => this.ImplementsMethod(typeof(IFilePlugin), "IdentifyAsync");

        #endregion
    }
}
