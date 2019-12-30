using System.Collections.Generic;
using Kontract.Interfaces.Plugins.Identifier;

namespace Kontract.Interfaces.Plugins.State.Font
{
    /// <summary>
    /// This is the v2 font adapter interface for creating font format plugins.
    /// </summary>
    public interface IFontAdapter2 : IFilePlugin
    {
        /// <summary>
        /// The list of characters provided by the font adapter to the UI.
        /// </summary>
        List<FontCharacter2> Characters { get; set; }

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
