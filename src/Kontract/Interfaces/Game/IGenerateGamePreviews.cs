using Kontract.Interfaces.Text;
using System.Drawing;

namespace Kontract.Interfaces.Game
{
    /// <summary>
    /// This is the game adapter interface for creating game preview plugins.
    /// </summary>
    public interface IGenerateGamePreviews
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="entry"></param>
        /// <returns></returns>
        Bitmap GeneratePreview(TextEntry entry);
    }
}
