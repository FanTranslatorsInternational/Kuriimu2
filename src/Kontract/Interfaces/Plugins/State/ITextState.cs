using System.Collections.Generic;
using Kontract.Interfaces.Plugins.State.Text;

namespace Kontract.Interfaces.Plugins.State
{
    public interface ITextState : IPluginState
    {
        IList<TextEntry> Texts { get; }
    }
}
