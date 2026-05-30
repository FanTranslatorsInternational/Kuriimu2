using Konnect.DataClasses.Management.Batch;

namespace Kuriimu2.Cmd.Models
{
    internal record ExportResult(ExportFailureReason Reason, BatchFileResult[] FileResults);
}
