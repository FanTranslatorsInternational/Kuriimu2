using System.IO;
using Kontract.Kompression;
using Kuriimu2.EtoForms.Forms.Dialogs.Extensions.Base;

namespace Kuriimu2.EtoForms.Forms.Dialogs.Extensions
{
    class CompressExtensionDialog:CompressionExtensionDialog
    {
        private const string CompressionKey_ = "Compression";

        protected override void ProcessCompression(ICompression compression, Stream input, Stream output)
        {
            compression.Compress(input, output);
        }

        protected override string GetExtensionName()
        {
            return Localize(CompressionKey_);
        }
    }
}
