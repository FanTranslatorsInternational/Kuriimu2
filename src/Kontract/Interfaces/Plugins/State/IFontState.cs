using System.Collections.Generic;
using Kontract.Models.Font;

namespace Kontract.Interfaces.Plugins.State
{
    public interface IFontState : IPluginState
    {
        /// <summary>
        /// The list of characters provided by the state.
        /// </summary>
        IReadOnlyList<CharacterInfo> Characters { get; }

        /// <summary>
        /// Character baseline.
        /// </summary>
        float Baseline { get; set; }

        /// <summary>
        /// Character descent line.
        /// </summary>
        float DescentLine { get; set; }
    }
}
