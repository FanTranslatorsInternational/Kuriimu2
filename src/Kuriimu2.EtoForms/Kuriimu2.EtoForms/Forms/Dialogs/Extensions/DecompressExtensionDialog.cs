using System.IO;
using Kontract.Kompression;

namespace Kuriimu2.EtoForms.Forms.Dialogs.Extensions
{
    class DecompressExtensionDialog : CompressExtensionDialog
    {
        private const string DecompressionKey_ = "Decompression";

        protected override void ProcessCompression(ICompression compression, Stream input, Stream output)
        {
            compression.Decompress(input, output);
        }

        protected override string GetExtensionName()
        {
            return Localize(DecompressionKey_);
        }
    }
}
