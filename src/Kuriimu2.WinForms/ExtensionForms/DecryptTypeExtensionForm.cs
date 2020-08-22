using System.IO;

namespace Kuriimu2.WinForms.ExtensionForms
{
    class DecryptTypeExtensionForm : CipherTypeExtensionForm
    {
        protected override void ProcessCipher(CipherStreamFactory cipherStreamFactory, Stream input, Stream output)
        {
            var cipherStream = cipherStreamFactory.CreateCipherStream(input);
            cipherStream.CopyTo(output);
        }
    }
}
