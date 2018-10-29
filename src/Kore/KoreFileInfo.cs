using System.ComponentModel;
using System.IO;
using Kontract.Interfaces;

namespace Kore
{
    /// <inheritdoc />
    /// <summary>
    /// KoreFileInfo is a simple file state tracking class designed to assist the UI.
    /// </summary>
    public class KoreFileInfo : INotifyPropertyChanged
    {
        /// <inheritdoc />
        /// <summary>
        /// The event handler for properties being changed.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        private bool _hasChanges;

        /// <summary>
        /// 
        /// </summary>
        public FileInfo FileInfo { get; set; }

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
        public string DisplayName => (FileInfo.Name + (HasChanges ? "*" : string.Empty)).Replace("_", "__");

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
        public void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
