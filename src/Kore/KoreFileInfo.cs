using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using Kontract.Interfaces;
using Kontract.Interfaces.Archive;
using Kontract.Interfaces.Common;

namespace Kore
{
    /// <inheritdoc />
    /// <summary>
    /// KoreFileInfo is a simple file state tracking class designed to assist the UI.
    /// </summary>
    public class KoreFileInfo : INotifyPropertyChanged
    {
        public KoreFileInfo ParentKfi { get; set; }

        public List<KoreFileInfo> ChildKfi { get; set; }

        public ArchiveFileInfo AFI { get; set; }

        public bool CanRequestFiles => Adapter is IMultipleFiles;

        public bool CanSave => Adapter is ISaveFiles;

        public bool CanCreate => Adapter is ICreateFiles;

        /// <inheritdoc />
        /// <summary>
        /// The event handler for properties being changed.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        private bool _hasChanges;

        /// <summary>
        /// 
        /// </summary>
        public StreamInfo StreamFileInfo { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public bool HasChanges
        {
            get => _hasChanges;
            set
            {
                if (value == _hasChanges) return;
                _hasChanges = value;
                OnPropertyChanged(nameof(HasChanges));
                OnPropertyChanged(nameof(DisplayName));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public ILoadFiles Adapter { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string DisplayName => Path.GetFileName(StreamFileInfo.FileName) + (HasChanges ? " *" : string.Empty);

        /// <summary>
        /// 
        /// </summary>
        public string Filter => Utilities.Common.GetAdapterFilter(Adapter);

        /// <summary>
        /// 
        /// </summary>
        public string Extension => Utilities.Common.GetAdapterExtension(Adapter);

        /// <summary>
        /// Allows the properties to notify the UI when their values have changed.
        /// </summary>
        /// <param name="propertyName">The name of the property that was changed.</param>
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void UpdateState(ArchiveFileState state)
        {
            if (AFI != null)
                AFI.State = state;
            if (ParentKfi != null)
                ParentKfi.UpdateState(state);

            HasChanges = false;
        }
    }
}