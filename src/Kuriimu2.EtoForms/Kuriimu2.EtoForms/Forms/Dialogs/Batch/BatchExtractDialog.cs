using Kore.Batch;
using Kore.Managers.Plugins;
using Serilog;

namespace Kuriimu2.EtoForms.Forms.Dialogs.Batch
{
    class BatchExtractDialog : BaseBatchDialog
    {
        #region Localization Keys

        private const string BatchExtractorKey_ = "BatchExtractor";

        private const string BatchExtractSourceEmptyKey_ = "BatchExtractSourceEmpty";
        private const string BatchExtractDestinationEmptyKey_ = "BatchExtractDestinationEmpty";

        #endregion

        protected override string SourceEmptyText { get; }
        protected override string DestinationEmptyText { get; }

        public BatchExtractDialog(IInternalFileManager fileManager) : base(fileManager)
        {
            Title = Localize(BatchExtractorKey_);
            SourceEmptyText = Localize(BatchExtractSourceEmptyKey_);
            DestinationEmptyText = Localize(BatchExtractDestinationEmptyKey_);
        }

        protected override BaseBatchProcessor InitializeBatchProcessor(IInternalFileManager fileManager, ILogger logger)
        {
            return new BatchExtractor(fileManager, logger);
        }
    }
}
