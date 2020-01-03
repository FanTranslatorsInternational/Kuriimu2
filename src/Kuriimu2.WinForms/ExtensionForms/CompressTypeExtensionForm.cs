using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kompression;

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
