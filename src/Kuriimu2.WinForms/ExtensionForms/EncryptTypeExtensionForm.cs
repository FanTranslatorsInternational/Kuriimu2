using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kuriimu2.WinForms.ExtensionForms
{
    class EncryptTypeExtensionForm:CipherTypeExtensionForm
    {
        protected override void ProcessCipher(Stream input, Stream cipherStream)
        {
            input.CopyTo(cipherStream);
        }
    }
}
