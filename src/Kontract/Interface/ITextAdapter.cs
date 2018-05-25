using System.Collections.Generic;
using System.Xml.Serialization;

namespace Kontract.Interface
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
        /// 0 for unlimted.
        /// </summary>
        int NameMaxLength { get; }

        // TODO: Determine if this member is necessary at all.
        /// <summary>
        /// The line endings used by the file format.
        /// </summary>
        string LineEndings { get; set; }
    }

    /// <summary>
    /// This interface allows the text adapter to add new entries through the UI.
    /// </summary>
    public interface IAddEntries
    {
        /// <summary>
        /// Creates a new entry and allows the plugin to provide its derived type.
        /// </summary>
        /// <returns>TextEntry or a derived type.</returns>
        TextEntry NewEntry();

        /// <summary>
        /// Adds a newly created entry to the file and allows the plugin to perform any required adding steps.
        /// </summary>
        /// <param name="entry"></param>
        /// <returns>True if the entry was added, False otherwise.</returns>
        bool AddEntry(TextEntry entry);
    }

    /// <summary>
    /// This interface allows the text afapter to rename entries through the UI making use of the NameList.
    /// </summary>
    public interface IRenameEntries
    {
        /// <summary>
        /// Renames an entry and allows the plugin to perform any required renaming steps.
        /// </summary>
        /// <param name="entry">The entry being renamed.</param>
        /// <param name="name">The new name to be assigned.</param>
        /// <returns>True if the entry was renamed, False otherwise.</returns>
        bool RenameEntry(TextEntry entry, string name);
    }

    /// <summary>
    /// This interface allows the text adapter to delete entries through the UI.
    /// </summary>
    public interface IDeleteEntries
    {
        /// <summary>
        /// Deletes an entry and allows the plugin to perform any required deletion steps.
        /// </summary>
        /// <param name="entry">The entry to be deleted.</param>
        /// <returns>True if the entry was successfully deleted, False otherwise.</returns>
        bool DeleteEntry(TextEntry entry);
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

    /// <summary>
    /// The base entry class.
    /// </summary>
    public class TextEntry
    {
        /// <summary>
        /// The entry's name.
        /// </summary>
        [XmlAttribute("name")]
        public virtual string Name { get; set; } = string.Empty;

        /// <summary>
        /// Stores the original text for the entry.
        /// </summary>
        [XmlElement("original")]
        public virtual string OriginalText { get; } = string.Empty;

        /// <summary>
        /// Stores the edited text for the entry.
        /// </summary>
        [XmlElement("edited")]
        public virtual string EditedText { get; set; } = string.Empty;

        /// <summary>
        /// Stores the note text for the entry.
        /// </summary>
        [XmlElement("notes")]
        public virtual string Notes { get; set; } = string.Empty;

        /// <summary>
        /// Limits the allowed text length that the entry can contain.
        /// 0 for unlimited.
        /// </summary>
        [XmlAttribute("max_length")]
        public virtual int MaxLength { get; } = 0;

        /// <summary>
        /// Determines whether this entry can be edited.
        /// </summary>
        [XmlIgnore]
        public virtual bool CanEdit { get; } = true;
    }
}
