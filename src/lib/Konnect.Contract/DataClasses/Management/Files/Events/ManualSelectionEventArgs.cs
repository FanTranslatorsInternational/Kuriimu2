using Konnect.Contract.Plugin.File;
using Konnect.Contract.Enums.Management.Files;

namespace Konnect.Contract.DataClasses.Management.Files.Events
{
    public class ManualSelectionEventArgs(IFilePlugin[] allFilePlugins, IFilePlugin[] filteredFilePlugins, SelectionStatus status)
        : EventArgs
    {
        public IEnumerable<IFilePlugin> FilePlugins { get; } = allFilePlugins;
        public IEnumerable<IFilePlugin> FilteredFilePlugins { get; } = filteredFilePlugins;
        public SelectionStatus SelectionStatus { get; } = status;

        public IFilePlugin? Result { get; set; }
    }
}
