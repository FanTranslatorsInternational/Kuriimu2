using Konnect.DataClasses.Management.Batch;

namespace Kuriimu2.Cmd.Models
{
    internal record ImportResult(ImportFailureReason Reason, BatchFileResult[] FileResults);
}
