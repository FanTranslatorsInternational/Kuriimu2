using System.Collections.Generic;
using System.Drawing;

namespace Kontract.Interfaces.Font
{
    /// <summary>
    /// This is the font adapter interface for creating font format plugins.
    /// </summary>
    public interface IFontAdapter : IPlugin
    {
        /// <summary>
        /// The list of characters provided by the font adapter to the UI.
        /// </summary>
        IEnumerable<FontCharacter> Characters { get; set; }

        /// <summary>
        /// The list of textures provided by the font adapter to the UI.
        /// </summary>
        List<Bitmap> Textures { get; set; }

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
