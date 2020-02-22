using System.IO;

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
