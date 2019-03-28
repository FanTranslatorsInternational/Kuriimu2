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
    [Export(typeof(ICipherAdapter))]
    [MenuStripExtension("AES", "128", "CTR", "LE")]
    public class Aes128CtrLeAdapter : ICipherAdapter
    {
        public EventHandler<RequestKeyEventArgs> RequestKey { get; set; }

        public string Name => throw new NotImplementedException();

        private byte[] OnRequestKey(string message, int keyLength, out string error)
        {
            error = string.Empty;

            var eventArgs = new RequestKeyEventArgs(message, keyLength);
            RequestKey?.Invoke(this, eventArgs);

            if (eventArgs.Data == null)
            {
                error = "Data not given.";
                return null;
            }

            if (eventArgs.Data.Length != keyLength)
            {
                error = "Data has no valid length.";
                return null;
            }

            return eventArgs.Data;
        }

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
            var key = OnRequestKey("AES128 Ctr Key", 16, out var error);
            if (key == null)
                return Task.Factory.StartNew(() =>
                {
                    progress.Report(new ProgressReport { Percentage = 0, Message = error });
                    return false;
                });

            var ctr = OnRequestKey("AES128 Ctr IV", 16, out error);
            if (ctr == null)
                return Task.Factory.StartNew(() =>
                {
                    progress.Report(new ProgressReport { Percentage = 0, Message = error });
                    return false;
                });

            return Task.Factory.StartNew(() =>
            {
                progress.Report(new ProgressReport { Percentage = 0, Message = decrypt ? "Decryption..." : "Encryption..." });

                using (var ecb = new CtrStream(decrypt ? input : output, key, ctr, true))
                {
                    var buffer = new byte[0x10000];
                    while (ecb.Position < ecb.Length)
                    {
                        var length = (int)Math.Min(0x10000, ecb.Length - ecb.Position);

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

                        progress.Report(new ProgressReport { Percentage = (double)ecb.Length / ecb.Position * 100, Message = decrypt ? "Decryption..." : "Encryption...", });
                    }
                }

                progress.Report(new ProgressReport { Percentage = 100, Message = decrypt ? "Decryption finished." : "Encryption finished." });

                return true;
            });
        }
    }
}
