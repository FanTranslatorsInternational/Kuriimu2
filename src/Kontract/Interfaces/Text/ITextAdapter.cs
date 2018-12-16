using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;

namespace Kontract.Interfaces
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
    /// This interface allows the text adapter to rename entries through the UI making use of the NameList.
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
    /// The base text entry class.
    /// </summary>
    public class TextEntry : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private string _originalText = string.Empty;
        private string _editedText = string.Empty;
        private string _notes = string.Empty;

        /// <inheritdoc />
        /// <summary>
        /// The event handler for properties being changed.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// The event handler for this entry getting edited.
        /// </summary>
        public event EventHandler Edited;

        /// <summary>
        /// The entry's name.
        /// </summary>
        [XmlAttribute("name")]
        public virtual string Name
        {
            get => _name;
            set
            {
                if (_name == value) return;
                _name = value;
                OnPropertyChanged(nameof(Name));
                OnEdited();
            }
        }

        /// <summary>
        /// Stores the original text for the entry.
        /// </summary>
        [XmlElement("original")]
        public virtual string OriginalText
        {
            get => _originalText;
            set
            {
                if (_originalText == value) return;
                _originalText = value;
                OnPropertyChanged(nameof(OriginalText));
                OnEdited();
            }
        }

        /// <summary>
        /// Stores the edited text for the entry.
        /// </summary>
        [XmlElement("edited")]
        public virtual string EditedText
        {
            get => _editedText;
            set
            {
                if (_editedText == value) return;
                _editedText = value;
                OnPropertyChanged(nameof(EditedText));
                OnEdited();
            }
        }

        /// <summary>
        /// Stores the note text for the entry.
        /// </summary>
        [XmlElement("notes")]
        public virtual string Notes
        {
            get => _notes;
            set
            {
                if (_notes == value) return;
                _notes = value;
                OnPropertyChanged(nameof(Notes));
                OnEdited();
            }
        }

        /// <summary>
        /// Limits the allowed text length that the entry can contain.
        /// 0 for unlimited.
        /// </summary>
        [XmlAttribute("max_length")]
        public virtual int MaxLength { get; set; } = 0;

        /// <summary>
        /// Determines whether this entry can be edited.
        /// </summary>
        [XmlIgnore]
        public virtual bool CanEdit { get; } = true;

        /// <summary>
        /// Allows the properties to notify the UI when their values have changed.
        /// </summary>
        /// <param name="propertyName">The name of the property that was changed.</param>
        public void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Raises the Edited event.
        /// </summary>
        protected virtual void OnEdited()
        {
            Edited?.Invoke(this, EventArgs.Empty);
        }
    }
}
