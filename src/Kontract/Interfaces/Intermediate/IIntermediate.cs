using Kontract.Interfaces.Common;

namespace Kontract.Interfaces.Intermediate
{
    /// <summary>
    /// A base interface for Kuriimu2's main tool strip.
    /// </summary>
    public interface IIntermediate : IPlugin
    {
        /// <summary>
        /// The name of the Intermediate Adapter.
        /// </summary>
        string Name { get; }
    }
}
