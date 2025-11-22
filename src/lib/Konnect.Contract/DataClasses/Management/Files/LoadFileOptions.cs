using Konnect.Contract.Management.Dialog;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Management.Streams;
using Konnect.Contract.Plugin.File;
using Konnect.Contract.Progress;
using Serilog;

namespace Konnect.Contract.DataClasses.Management.Files;

public class LoadFileOptions
{
    public IFileState ParentFileState { get; set; }

    public IStreamManager StreamManager { get; set; }

    public IFileManager FileManager { get; set; }

    public IFilePlugin? Plugin { get; set; }

    public IProgressContext Progress { get; set; }

    public IDialogManager? DialogManager { get; set; }

    public bool AllowManualSelection { get; set; }

    public ILogger Logger { get; set; }
}