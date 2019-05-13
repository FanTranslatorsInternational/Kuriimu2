using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
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
        /// <summary>
        /// If the file was opened from an archive, this property is set to the parent Archive KFI
        /// </summary>
        public KoreFileInfo ParentKfi { get; set; }

        /// <summary>
        /// If this file is an archive, it will track the KFIs that get opened from it
        /// </summary>
        public List<KoreFileInfo> ChildKfi { get; set; }

        //TODO: AFI in KoreFileInfo subject to remove?
        /// <summary>
        /// This files ArchiveFileInfo to get access to parent states
        /// </summary>
        public ArchiveFileInfo AFI { get; set; }

        /// <summary>
        /// If the plugin, the file was loaded with, can open more files than the one in this KoreFileInfo
        /// </summary>
        //public bool CanRequestFiles => Adapter is IMultipleFiles;

        /// <summary>
        /// If the plugin, the file was loaded with, can save
        /// </summary>
        public bool CanSave => Adapter is ISaveFiles;

        /// <summary>
        /// If the plugin, the file was loaded with, can create a new file from scratch
        /// </summary>
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
            get => _hasChanges || (ChildKfi?.Any(x => x.HasChanges) ?? false);
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
        /// The full path of the opened file, cascaded through its parents, if opened through one or several archives
        /// </summary>
        public string FullPath => Path.Combine(ParentKfi?.FullPath ?? "", StreamFileInfo.FileName);

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