using Kore.Batch;
using Kore.Managers.Plugins;
using Serilog;

namespace Kuriimu2.EtoForms.Forms.Dialogs.Batch
{
    class BatchInjectDialog : BaseBatchDialog
    {
        #region Localization Keys

        private const string BatchInjectorKey_ = "BatchInjector";

        private const string BatchInjectSourceEmptyKey_ = "BatchInjectSourceEmpty";
        private const string BatchInjectDestinationEmptyKey_ = "BatchInjectDestinationEmpty";

        #endregion

        protected override string SourceEmptyText { get; }
        protected override string DestinationEmptyText { get; }

        public BatchInjectDialog(IInternalFileManager fileManager) : base(fileManager)
        {
            Title = Localize(BatchInjectorKey_);
            SourceEmptyText = Localize(BatchInjectSourceEmptyKey_);
            DestinationEmptyText = Localize(BatchInjectDestinationEmptyKey_);
        }

        protected override BaseBatchProcessor InitializeBatchProcessor(IInternalFileManager fileManager, ILogger logger)
        {
            return new BatchInjector(fileManager, logger);
        }
    }
}
