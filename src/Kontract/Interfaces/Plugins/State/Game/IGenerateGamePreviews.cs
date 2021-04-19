using System;
using System.Drawing;
using Kontract.Interfaces.Plugins.State.Text;
using Kontract.Models.Text;

namespace Kontract.Interfaces.Plugins.State.Game
{
    /// <summary>
    /// This is the game adapter interface for creating game preview plugins.
    /// </summary>
    [Obsolete("Override IGameAdapter.GeneratePreview instead")]
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
