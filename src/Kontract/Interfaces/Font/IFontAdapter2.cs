using System.Collections.Generic;
using Kontract.Interfaces.Common;

namespace Kontract.Interfaces.Font
{
    /// <summary>
    /// This is the v2 font adapter interface for creating font format plugins.
    /// </summary>
    public interface IFontAdapter2 : IPlugin
    {
        /// <summary>
        /// The list of characters provided by the font adapter to the UI.
        /// </summary>
        IEnumerable<FontCharacter2> Characters { get; set; }

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
