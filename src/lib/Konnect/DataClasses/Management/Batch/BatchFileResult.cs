using Konnect.Contract.DataClasses.Management.Dialog;

namespace Konnect.DataClasses.Management.Batch
{
    public record BatchFileResult(string FilePath, BatchFileStatus Status, IList<DialogField> DialogFields);
}
