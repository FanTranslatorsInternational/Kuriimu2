using System.IO;

namespace Kuriimu2.WinForms.ExtensionForms
{
    class EncryptTypeExtensionForm : CipherTypeExtensionForm
    {
        protected override void ProcessCipher(CipherStreamFactory cipherStreamFactory, Stream input, Stream output)
        {
            var cipherStream = cipherStreamFactory.CreateCipherStream(output);
            input.CopyTo(cipherStream);
            cipherStream.Flush();
        }
    }
}
