using Kontract;
using Kontract.Interfaces.Common;
using Kontract.Interfaces.Intermediate;
using Kryptography.AES;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Kontract.Attributes;

namespace plugin_krypto_aes.Ecb
{
    [Export(typeof(IPlugin))]
    [MenuStripExtension("AES", "128", "ECB")]
    public class Aes128EcbAdapter : ICipherAdapter
    {
        public event EventHandler<RequestDataEventArgs> RequestData;

        public string Name => throw new NotImplementedException();

        private byte[] OnRequestKey(string message, int keyLength, out string error)
            => RequestMethods.RequestKey((args) => RequestData?.Invoke(this, args), message, keyLength, out error);

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
            var key = OnRequestKey("AES128 ECB Key", 16, out var error);
            if (key == null)
                return Task.Factory.StartNew(() =>
                {
                    progress.Report(new ProgressReport { Percentage = 0, Message = error });
                    return false;
                });

            return Task.Factory.StartNew(() =>
            {
                progress.Report(new ProgressReport { Percentage = 0, Message = decrypt ? "Decryption..." : "Encryption..." });

                using (var ecb = new EcbStream(decrypt ? input : output, key))
                {
                    var buffer = new byte[0x10000];
                    var totalLength = decrypt ? ecb.Length : input.Length;
                    while (ecb.Position < totalLength)
                    {
                        var length = (int)Math.Min(0x10000, totalLength - ecb.Position);

                        if (decrypt)
                        {
                            ecb.Read(buffer, 0, length);
                            output.Write(buffer, 0, length);
                        }
                        else
                        {
                            input.Read(buffer, 0, length);
                            ecb.Write(buffer, 0, length);
                        }

                        progress.Report(new ProgressReport { Percentage = (double)ecb.Position / totalLength * 100, Message = decrypt ? "Decryption..." : "Encryption...", });
                    }
                }

                progress.Report(new ProgressReport { Percentage = 100, Message = decrypt ? "Decryption finished." : "Encryption finished." });

                return true;
            });
        }
    }
}
