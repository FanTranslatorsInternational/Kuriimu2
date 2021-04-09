using Kontract.Interfaces.Logging;
using Kore.Batch;
using Kore.Managers.Plugins;

namespace Kuriimu2.WinForms.BatchForms
{
    class BatchInjectionForm : BaseBatchForm
    {
        protected override string SourceEmptyText { get; } = "Select a directory with files to inject.";
        protected override string DestinationEmptyText { get; } = "Select a directory with directories to inject to the files.";

        public BatchInjectionForm(IInternalPluginManager pluginManager) : base(pluginManager)
        {
        }

        protected override BaseBatchProcessor InitializeBatchProcessor(IInternalPluginManager pluginManager, IConcurrentLogger logger)
        {
            return new BatchInjector(pluginManager, logger);
        }
    }
}
