using System.Collections.Generic;
using System.Drawing;
using Kontract.Interfaces.Plugins.Identifier;

namespace Kontract.Interfaces.Plugins.State.Font
{
    /// <summary>
    /// This is the font adapter interface for creating font format plugins.
    /// </summary>
    public interface IFontAdapter : IFilePlugin
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
