using System.Collections.Generic;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Progress;

namespace Kontract.Models.Context
{
    /// <summary>
    /// The class containing all environment instances for a load process in <see cref="IPluginManager"/>.
    /// </summary>
    public class LoadFileContext
    {
        /// <summary>
        /// The options for this load process.
        /// </summary>
        public IList<string> Options { get; set; }

        /// <summary>
        /// The context to report progress through.
        /// </summary>
        public IProgressContext Progress { get; set; }
    }
}
