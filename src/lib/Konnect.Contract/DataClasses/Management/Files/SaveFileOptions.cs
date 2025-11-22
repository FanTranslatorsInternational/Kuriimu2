using Konnect.Contract.Management.Dialog;
using Konnect.Contract.Progress;
using Serilog;

namespace Konnect.Contract.DataClasses.Management.Files;

public class SaveFileOptions
{
    public IProgressContext Progress { get; set; }

    public IDialogManager? DialogManager { get; set; }

    public ILogger Logger { get; set; }
}