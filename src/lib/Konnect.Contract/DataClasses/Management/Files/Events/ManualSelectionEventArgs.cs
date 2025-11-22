using Konnect.Contract.Plugin.File;
using Konnect.Contract.Enums.Management.Files;

namespace Konnect.Contract.DataClasses.Management.Files.Events;

public class ManualSelectionEventArgs(IFilePlugin[] allFilePlugins, IFilePlugin[] filteredFilePlugins, SelectionStatus status)
    : EventArgs
{
    public IFilePlugin[] FilePlugins { get; } = allFilePlugins;
    public IFilePlugin[] FilteredFilePlugins { get; } = filteredFilePlugins;
    public SelectionStatus SelectionStatus { get; } = status;

    public IFilePlugin? Result { get; set; }
}