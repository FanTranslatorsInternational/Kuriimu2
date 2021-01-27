using System.IO;
using Kontract.Kompression;

namespace Kuriimu2.EtoForms.Forms.Dialogs.Extensions
{
    class DecompressExtensionDialog : CompressExtensionDialog

    {
        protected override void ProcessCompression(ICompression compression, Stream input, Stream output)
        {
            compression.Decompress(input, output);
        }
    }
}
