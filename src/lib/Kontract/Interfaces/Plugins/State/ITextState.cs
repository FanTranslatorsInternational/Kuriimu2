using System.Collections.Generic;
using Kontract.Interfaces.Plugins.State.Text;
using Kontract.Models.Plugins.State.Text;

namespace Kontract.Interfaces.Plugins.State
{
    /// <summary>
    /// Marks the state to be a text format and exposes properties to retrieve and modify text data from the state.
    /// </summary>
    public interface ITextState : IPluginState
    {
        /// <summary>
        /// The loaded texts of the file format.
        /// </summary>
        IList<TextInfo> Texts { get; }

        #region Optional feature checks

        /// <summary>
        /// If the plugin allows adding entries.
        /// </summary>
        public bool CanAddEntry => this is IAddEntries;

        /// <summary>
        /// If the plugin allows deleting entries.
        /// </summary>
        public bool CanDeleteEntry => this is IDeleteEntries;

        /// <summary>
        /// If the plugin allows renaming entries.
        /// </summary>
        public bool CanRenameEntry => this is IRenameEntries;

        #endregion

        #region Optional feature casting defaults

        /// <summary>
        /// Casts and executes <see cref="IAddEntries.CreateNewEntry"/>.
        /// </summary>
        /// <returns><see cref="TextInfo"/> or a derived type.</returns>
        TextInfo AttemptCreateNewEntry() => ((IAddEntries)this).CreateNewEntry();

        /// <summary>
        /// Casts and executes <see cref="IAddEntries.AddEntry"/>.
        /// </summary>
        /// <param name="info"></param>
        /// <returns><c>true</c>, if the info was added, <c>false</c> otherwise.</returns>
        bool AttemptAddEntry(TextInfo info) => ((IAddEntries)this).AddEntry(info);

        /// <summary>
        /// Casts and executes <see cref="IDeleteEntries.DeleteEntry"/>.
        /// </summary>
        /// <param name="info">The info to be deleted.</param>
        /// <returns><c>true</c>, if the info was successfully deleted, <c>false</c> otherwise.</returns>
        bool AttemptDeleteEntry(TextInfo info) => ((IDeleteEntries)this).DeleteEntry(info);

        /// <summary>
        /// Casts and executes <see cref="IRenameEntries.RenameEntry"/>.
        /// </summary>
        /// <param name="info">The info being renamed.</param>
        /// <param name="name">The new name to be assigned.</param>
        /// <returns><c>true</c>, if the info was renamed, <c>false</c> otherwise.</returns>
        bool AttemptRenameEntry(TextInfo info, string name) => ((IRenameEntries)this).RenameEntry(info, name);

        #endregion
    }
}
