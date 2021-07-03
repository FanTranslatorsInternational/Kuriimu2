using System.IO;
using Kuriimu2.EtoForms.Forms.Dialogs.Extensions.Base;

namespace Kuriimu2.EtoForms.Forms.Dialogs.Extensions
{
    class DecryptExtensionDialog : CipherExtensionDialog
    {
        private const string DecryptionKey_ = "Decryption";

        protected override void ProcessCipher(CipherStreamFactory cipherStreamFactory, Stream input, Stream output)
        {
            var cipherStream = cipherStreamFactory.CreateCipherStream(input);
            cipherStream.CopyTo(output);
        }

        protected override string GetExtensionName()
        {
            return Localize(DecryptionKey_);
        }
    }
}
