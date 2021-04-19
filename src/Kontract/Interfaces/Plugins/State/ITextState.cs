using System;
using System.Collections.Generic;
using Kontract.Extensions;
using Kontract.Interfaces.Plugins.State.Text;
using Kontract.Models.Text;

namespace Kontract.Interfaces.Plugins.State
{
    public interface ITextState : IPluginState
    {
        IList<TextEntry> Texts { get; }
        
        #region Optional features

        /// <summary>
        /// Creates a new entry and allows the plugin to provide its derived type.
        /// </summary>
        /// <returns>TextEntry or a derived type.</returns>
        TextEntry NewEntry()
        {
            throw new InvalidOperationException();
        }

        /// <summary>
        /// Adds a newly created entry to the file and allows the plugin to perform any required adding steps.
        /// </summary>
        /// <param name="entry"></param>
        /// <returns>True if the entry was added, False otherwise.</returns>
        bool AddEntry(TextEntry entry)
        {
            throw new InvalidOperationException();
        }

        /// <summary>
        /// Deletes an entry and allows the plugin to perform any required deletion steps.
        /// </summary>
        /// <param name="entry">The entry to be deleted.</param>
        /// <returns>True if the entry was successfully deleted, False otherwise.</returns>
        bool DeleteEntry(TextEntry entry)
        {
            throw new InvalidOperationException();
        }

        /// <summary>
        /// Renames an entry and allows the plugin to perform any required renaming steps.
        /// </summary>
        /// <param name="entry">The entry being renamed.</param>
        /// <param name="name">The new name to be assigned.</param>
        /// <returns>True if the entry was renamed, False otherwise.</returns>
        bool RenameEntry(TextEntry entry, string name)
        {
            throw new InvalidOperationException();
        }
        
        // TODO: Figure out how to best implement this feature with WPF.
        /// <summary>
        /// Opens the extended properties dialog for an entry.
        /// </summary>
        /// <param name="entry">The entry to view and/or edit extended properties for.</param>
        /// <returns>True if changes were made, False otherwise.</returns>
        bool ShowEntryProperties(TextEntry entry)
        {
            throw new InvalidOperationException();
        }
        
        #endregion
        
        #region Optional feature checks
        
        //TODO check NewEntry too?
        public bool CanAddEntry => this.ImplementsMethod(typeof(ITextState), nameof(AddEntry));
        public bool CanDeleteEntry => this.ImplementsMethod(typeof(ITextState), nameof(DeleteEntry));
        public bool CanRenameEntry => this.ImplementsMethod(typeof(ITextState), nameof(RenameEntry));
        public bool CanShowEntryProperties => this.ImplementsMethod(typeof(ITextState), nameof(ShowEntryProperties));
        
        #endregion
    }
}
