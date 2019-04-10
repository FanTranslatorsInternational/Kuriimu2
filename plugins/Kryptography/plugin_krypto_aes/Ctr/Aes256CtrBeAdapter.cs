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

namespace plugin_krypto_aes.Ctr
{
    [Export(typeof(IPlugin))]
    [MenuStripExtension("AES", "256", "CTR", "BE")]
    public class Aes256CtrBeAdapter : ICipherAdapter
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
            var key = OnRequestKey("AES128 Ctr Key", 32, out var error);
            if (key == null)
                return Task.Factory.StartNew(() =>
                {
                    progress.Report(new ProgressReport { Percentage = 0, Message = error });
                    return false;
                });

            var ctr = OnRequestKey("AES128 Ctr IV", 32, out error);
            if (ctr == null)
                return Task.Factory.StartNew(() =>
                {
                    progress.Report(new ProgressReport { Percentage = 0, Message = error });
                    return false;
                });

            return Task.Factory.StartNew(() =>
            {
                progress.Report(new ProgressReport { Percentage = 0, Message = decrypt ? "Decryption..." : "Encryption..." });

                using (var cs = new CtrStream(decrypt ? input : output, key, ctr, false))
                {
                    var buffer = new byte[0x10000];
                    while (cs.Position < cs.Length)
                    {
                        var length = (int)Math.Min(0x10000, cs.Length - cs.Position);

                        if (decrypt)
                        {
                            cs.Read(buffer, 0, length);
                            output.Write(buffer, 0, length);
                        }
                        else
                        {
                            input.Read(buffer, 0, length);
                            cs.Write(buffer, 0, length);
                        }

                        progress.Report(new ProgressReport { Percentage = (double)cs.Position / cs.Length * 100, Message = decrypt ? "Decryption..." : "Encryption...", });
                    }
                }

                progress.Report(new ProgressReport { Percentage = 100, Message = decrypt ? "Decryption finished." : "Encryption finished." });

                return true;
            });
        }
    }
}
