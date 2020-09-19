using System.IO;
using Kontract.Kompression;

namespace Kuriimu2.WinForms.ExtensionForms
{
    class DecompressTypeExtensionForm : CompressionTypeExtensionForm
    {
        protected override void ProcessCompression(ICompression compression, Stream input, Stream output)
        {
            compression.Decompress(input, output);
        }
    }
}
