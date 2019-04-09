using Kontract;
using Kontract.Attributes;
using Kontract.Interfaces.Common;
using Kontract.Interfaces.Intermediate;
using Kryptography;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace plugin_krypto_xor
{
    [Export(typeof(IPlugin))]
    [MenuStripExtension("General", "Xor")]
    public class XorAdapter : ICipherAdapter
    {
        public event EventHandler<RequestDataEventArgs> RequestData;

        public string Name => "Xor";

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
            var key = OnRequestKey("XOR Key", -1, out var error);
            if (key == null)
                return Task.Factory.StartNew(() =>
                {
                    progress.Report(new ProgressReport { Percentage = 0, Message = error });
                    return false;
                });

            return Task.Factory.StartNew(() =>
            {
                progress.Report(new ProgressReport { Percentage = 0, Message = decrypt ? "Decryption..." : "Encryption..." });

                using (var xor = new XorStream(decrypt ? input : output, key))
                {
                    var buffer = new byte[0x10000];
                    while (xor.Position < xor.Length)
                    {
                        var length = (int)Math.Min(0x10000, xor.Length - xor.Position);

                        if (decrypt)
                        {
                            xor.Read(buffer, 0, length);
                            output.Write(buffer, 0, length);
                        }
                        else
                        {
                            input.Read(buffer, 0, length);
                            xor.Write(buffer, 0, length);
                        }

                        progress.Report(new ProgressReport { Percentage = (double)xor.Length / xor.Position * 100, Message = decrypt ? "Decryption..." : "Encryption...", });
                    }
                }

                progress.Report(new ProgressReport { Percentage = 100, Message = decrypt ? "Decryption finished." : "Encryption finished." });

                return true;
            });
        }
    }
}
