using Kore.Batch;
using Kore.Managers.Plugins;
using Serilog;

namespace Kuriimu2.EtoForms.Forms.Dialogs.Batch
{
    class BatchInjectDialog : BaseBatchDialog
    {
        protected override string SourceEmptyText { get; } = "Select a directory with files to inject.";
        protected override string DestinationEmptyText { get; } = "Select a directory with directories to inject to the files.";

        public BatchInjectDialog(IInternalPluginManager pluginManager) : base(pluginManager)
        {
        }

        protected override BaseBatchProcessor InitializeBatchProcessor(IInternalPluginManager pluginManager, ILogger logger)
        {
            return new BatchInjector(pluginManager, logger);
        }
    }
}
