using System;
using System.Collections.Generic;
using System.Drawing;
using Kontract.Extensions;
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
        PluginMetadata MetaData { get; }

        //TODO: Implement game adapters
        /// <summary>
        /// 
        /// </summary>
        Guid PluginId { get; }

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
        
        #region Optional features
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="entry"></param>
        /// <returns></returns>
        Bitmap GeneratePreview(TextEntry entry)
        {
            throw new InvalidOperationException();
        }
        
        #endregion
        
        #region Optional feature support checks
        
        public bool CanGeneratePreview => this.ImplementsMethod(typeof(IGameAdapter), nameof(GeneratePreview));
        
        #endregion
    }
}
