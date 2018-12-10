using Kontract.Interfaces.Text;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;

namespace Kontract.Interfaces.Text
{
    /// <summary>
    /// This is the text adapter interface for creating text format plugins.
    /// </summary>
    public interface ITextAdapter
    {
        /// <summary>
        /// The list of entries provided by the text adapter to the UI.
        /// </summary>
        IEnumerable<TextEntry> Entries { get; }

        /// <summary>
        /// A regular expression that new names must match.
        /// Use @".*" to accept any character.
        /// </summary>
        string NameFilter { get; }

        /// <summary>
        /// The maximum length that entry names can store.
        /// Longer names will be truncated.
        /// 0 for unlimited.
        /// </summary>
        int NameMaxLength { get; }

        // TODO: Determine if this member is necessary at all.
        /// <summary>
        /// The line endings used by the file format.
        /// </summary>
        string LineEndings { get; set; }
    }

    /// <summary>
    /// Entries provide an extended properties dialog?
    /// </summary>
    public interface IEntriesHaveExtendedProperties
    {
        // TODO: Figure out how to best implement this feature with WPF.
        /// <summary>
        /// Opens the extended properties dialog for an entry.
        /// </summary>
        /// <param name="entry">The entry to view and/or edit extended properties for.</param>
        /// <returns>True if changes were made, False otherwise.</returns>
        bool ShowEntryProperties(TextEntry entry);
    }
}
