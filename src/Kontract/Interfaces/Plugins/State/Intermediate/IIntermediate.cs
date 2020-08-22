using Kontract.Interfaces.Plugins.Identifier;

namespace Kontract.Interfaces.Plugins.State.Intermediate
{
    /// <summary>
    /// A base interface for Kuriimu2's main tool strip.
    /// </summary>
    public interface IIntermediate : IFilePlugin
    {
        /// <summary>
        /// The name of the Intermediate Adapter.
        /// </summary>
        string Name { get; }
    }
}
