using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace Kontract.Models.Text
{
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
        public virtual int MaxLength { get; set; }

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
