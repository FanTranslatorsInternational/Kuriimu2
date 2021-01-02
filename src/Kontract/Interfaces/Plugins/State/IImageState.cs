using System.Collections.Generic;
using Kontract.Kanvas;
using Kontract.Models.Image;

namespace Kontract.Interfaces.Plugins.State
{
    public interface IImageState : IPluginState
    {
        EncodingDefinition EncodingDefinition { get; }

        IList<IKanvasImage> Images { get; }
    }
}
