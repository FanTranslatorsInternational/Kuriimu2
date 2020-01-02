using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kuriimu2.WinForms.ExtensionForms
{
    class DecryptTypeExtensionForm : CipherTypeExtensionForm
    {
        protected override void ProcessCipher(Stream input, Stream cipherStream)
        {
            cipherStream.CopyTo(input);
        }
    }
}
