using Kontract.Interfaces.Intermediate;
using Kryptography.AES;
using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading.Tasks;
using Kontract.Attributes;
using Kontract.Interfaces;
using Kontract.Models;
using Kontract.Models.Intermediate;

namespace plugin_krypto_aes.Cbc
{
    [Export(typeof(IPlugin))]
    [MenuStripExtension("AES", "128", "CBC")]
    public class Aes128CbcAdapter : ICipherAdapter
    {
        public event EventHandler<RequestDataEventArgs> RequestData;

        public string Name => "Aes128 CBC";

        private byte[] OnRequestKey(string message, int keyLength, string requestId, out string error)
            => RequestMethods.RequestKey((args) => RequestData?.Invoke(this, args), message, keyLength, requestId, out error);

        public Task<bool> Decrypt(Stream toDecrypt, Stream decryptInto, IProgress<ProgressReport> progress)
        {
            return DoCipher(toDecrypt, decryptInto, progress, true);
        }

        public Task<bool> Encrypt(Stream toEncrypt, Stream encryptInto, IProgress<ProgressReport> progress)
        {
            return DoCipher(toEncrypt, encryptInto, progress, false);
        }

        private Task<bool> DoCipher(Stream input, Stream output, IProgress<ProgressReport> progress, bool decrypt)
        {
            var requestId = Guid.NewGuid().ToString("N");

            var key = OnRequestKey("AES128 CBC Key", 16, requestId,out var error);
            if (key == null)
                return Task.Factory.StartNew(() =>
                {
                    progress.Report(new ProgressReport { Percentage = 0, Message = error });
                    return false;
                });

            var iv = OnRequestKey("AES128 CBC IV", 16, requestId, out error);
            if (iv == null)
                return Task.Factory.StartNew(() =>
                {
                    progress.Report(new ProgressReport { Percentage = 0, Message = error });
                    return false;
                });

            return Task.Factory.StartNew(() =>
            {
                progress.Report(new ProgressReport { Percentage = 0, Message = decrypt ? "Decryption..." : "Encryption..." });

                using (var cbc = new CbcStream(decrypt ? input : output, key, iv))
                {
                    var buffer = new byte[0x10000];
                    var totalLength = decrypt ? cbc.Length : input.Length;
                    while (cbc.Position < totalLength)
                    {
                        var length = (int)Math.Min(0x10000, totalLength - cbc.Position);

                        if (decrypt)
                        {
                            cbc.Read(buffer, 0, length);
                            output.Write(buffer, 0, length);
                        }
                        else
                        {
                            input.Read(buffer, 0, length);
                            cbc.Write(buffer, 0, length);
                        }

                        progress.Report(new ProgressReport { Percentage = (double)cbc.Position / totalLength * 100, Message = decrypt ? "Decryption..." : "Encryption...", });
                    }
                }

                progress.Report(new ProgressReport { Percentage = 100, Message = decrypt ? "Decryption finished." : "Encryption finished." });

                return true;
            });
        }
    }
}
