using System.IO;
using Kuriimu2.EtoForms.Forms.Dialogs.Extensions.Base;

namespace Kuriimu2.EtoForms.Forms.Dialogs.Extensions
{
    class EncryptExtensionsDialog:CipherExtensionDialog
    {
        private const string EncryptionKey_ = "Encryption";

        protected override void ProcessCipher(CipherStreamFactory cipherStreamFactory, Stream input, Stream output)
        {
            var cipherStream = cipherStreamFactory.CreateCipherStream(output);
            input.CopyTo(cipherStream);
            cipherStream.Flush();
        }

        protected override string GetExtensionName()
        {
            return Localize(EncryptionKey_);
        }
    }
}
