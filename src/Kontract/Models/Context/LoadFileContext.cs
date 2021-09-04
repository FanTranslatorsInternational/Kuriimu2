using System;
using System.Collections.Generic;
using Kontract.Interfaces.Managers;
using Serilog;

namespace Kontract.Models.Context
{
    /// <summary>
    /// The class containing all environment instances for a load process in <see cref="IBaseFileManager"/>.
    /// </summary>
    public class LoadFileContext
    {
        /// <summary>
        /// The options for this load process.
        /// </summary>
        public IList<string> Options { get; set; }

        /// <summary>
        /// The preset id of the plugin to use to load the file.
        /// </summary>
        public Guid PluginId { get; set; } = Guid.Empty;

        /// <summary>
        /// The logger to use for the load file operation.
        /// </summary>
        public ILogger Logger { get; set; }
    }
}
