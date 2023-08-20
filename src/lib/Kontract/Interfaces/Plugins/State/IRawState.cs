using System.IO;

namespace Kontract.Interfaces.Plugins.State
{
    /// <summary>
    /// Marks the state to be a raw format and exposes the raw binary data from the state.
    /// </summary>
    public interface IRawState : IPluginState
    {
        /// <summary>
        /// The raw binary data of the format.
        /// </summary>
        Stream FileStream { get; }
    }
}
