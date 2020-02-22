using System.IO;

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
