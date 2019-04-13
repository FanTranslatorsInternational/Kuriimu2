using System.Collections.Generic;
using System.Drawing;
using Kontract.Interfaces.Common;
using Kontract.Interfaces.Text;

namespace Kontract.Interfaces.Game
{
    /// <summary>
    /// This is the game adapter interface for creating game formatting plugins.
    /// </summary>
    public interface IGameAdapter : IPlugin
    {
        /// <summary>
        /// 
        /// </summary>
        //string ID { get; }

        /// <summary>
        /// 
        /// </summary>
        //string Name { get; }

        /// <summary>
        /// 
        /// </summary>
        string IconPath { get; }

        /// <summary>
        /// 
        /// </summary>
        string Filename { get; set; }

        /// <summary>
        /// The list of game text entries provided by the game adapter to the UI.
        /// </summary>
        //IEnumerable<TextEntry> Entries { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="entries"></param>
        //void LoadEntries(IEnumerable<TextEntry> entries);

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        //IEnumerable<TextEntry> SaveEntries();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="entry"></param>
        /// <returns></returns>
        Bitmap GeneratePreview(TextEntry entry);
    }

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
