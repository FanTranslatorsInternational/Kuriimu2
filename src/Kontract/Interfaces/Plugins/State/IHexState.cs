using System.IO;

namespace Kontract.Interfaces.Plugins.State
{
    public interface IHexState : IPluginState
    {
        Stream FileStream { get; }
    }
}
