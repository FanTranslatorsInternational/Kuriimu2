using Kontract.Interfaces.Logging;
using Kore.Batch;
using Kore.Managers.Plugins;

namespace Kuriimu2.WinForms.BatchForms
{
    class BatchExtractionForm : BaseBatchForm
    {
        protected override string SourceEmptyText { get; } = "Select a directory with files to extract.";
        protected override string DestinationEmptyText { get; } = "Select a directory to extract the files to.";

        public BatchExtractionForm(IInternalPluginManager pluginManager) : base(pluginManager)
        {
        }

        protected override BaseBatchProcessor InitializeBatchProcessor(IInternalPluginManager pluginManager, IConcurrentLogger logger)
        {
            return new BatchExtractor(pluginManager, logger);
        }
    }
}
