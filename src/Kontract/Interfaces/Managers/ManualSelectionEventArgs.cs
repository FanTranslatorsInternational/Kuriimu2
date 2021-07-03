using System;
using System.Collections.Generic;
using System.Linq;
using Kontract.Interfaces.Plugins.Identifier;

namespace Kontract.Interfaces.Managers
{
    public class ManualSelectionEventArgs : EventArgs
    {
        public IEnumerable<IFilePlugin> FilePlugins { get; }
        public IEnumerable<IFilePlugin> FilteredFilePlugins { get; }
        public SelectionStatus SelectionStatus { get; }

        public IFilePlugin Result { get; set; }

        public ManualSelectionEventArgs(IEnumerable<IFilePlugin> allFilePlugins, IEnumerable<IFilePlugin> filteredFilePlugins, SelectionStatus status)
        {
            FilePlugins = allFilePlugins;
            FilteredFilePlugins = filteredFilePlugins;
            SelectionStatus = status;
        }
    }

    public enum SelectionStatus
    {
        All,
        MultipleMatches,
        NonIdentifiable
    }
}
