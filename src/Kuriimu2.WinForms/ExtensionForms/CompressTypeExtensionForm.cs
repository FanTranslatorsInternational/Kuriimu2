using System.IO;
using Kontract.Kompression;

namespace Kuriimu2.WinForms.ExtensionForms
{
    class CompressTypeExtensionForm : CompressionTypeExtensionForm
    {
        protected override void ProcessCompression(ICompression compression, Stream input, Stream output)
        {
            compression.Compress(input, output);
        }
    }
}
