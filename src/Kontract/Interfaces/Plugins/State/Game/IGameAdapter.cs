using System;
using System.Collections.Generic;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Models;
using Kontract.Models.Text;

namespace Kontract.Interfaces.Plugins.State.Game
{
    /// <summary>
    /// This is the game adapter interface for creating game formatting plugins.
    /// </summary>
    public interface IGameAdapter : IGamePlugin
    {

        /// <summary>
        /// 
        /// </summary>
        string Name { get; }

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
        IEnumerable<TextEntry> Entries { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="entries"></param>
        void LoadEntries(IEnumerable<TextEntry> entries);

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        IEnumerable<TextEntry> SaveEntries();
    }
}
