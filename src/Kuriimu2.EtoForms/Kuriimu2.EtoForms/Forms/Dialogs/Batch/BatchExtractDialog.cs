using Kore.Batch;
using Kore.Managers.Plugins;
using Serilog;

namespace Kuriimu2.EtoForms.Forms.Dialogs.Batch
{
    class BatchExtractDialog : BaseBatchDialog
    {
        protected override string SourceEmptyText { get; } = "Select a directory with files to extract.";
        protected override string DestinationEmptyText { get; } = "Select a directory to extract the files to.";

        public BatchExtractDialog(IInternalPluginManager pluginManager) : base(pluginManager)
        {
        }

        protected override BaseBatchProcessor InitializeBatchProcessor(IInternalPluginManager pluginManager, ILogger logger)
        {
            return new BatchExtractor(pluginManager, logger);
        }
    }
}
