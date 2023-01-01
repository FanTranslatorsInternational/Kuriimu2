using System.Collections.Generic;
using Kontract.Kanvas.Interfaces;

namespace Kontract.Interfaces.Plugins.State
{
    /// <summary>
    /// Marks the state to be an image format and exposes properties to retrieve and modify image data from the state.
    /// </summary>
    public interface IImageState : IPluginState
    {
        /// <summary>
        /// The list of images in the format
        /// </summary>
        IList<IImageInfo> Images { get; }
    }
}
