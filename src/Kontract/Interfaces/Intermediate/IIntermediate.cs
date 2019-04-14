using Kontract.Interfaces.Common;

namespace Kontract.Interfaces.Intermediate
{
    /// <summary>
    /// 
    /// </summary>
    public interface IIntermediate : IPlugin
    {
        /// <summary>
        /// The name of the Intermediate Adapter
        /// </summary>
        string Name { get; }
    }
}
