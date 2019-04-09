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
    [MenuStripExtension("AES", "256", "XTS", "LE")]
    public class Aes256XtsLeAdapter : ICipherAdapter
    {
        public event EventHandler<RequestDataEventArgs> RequestData;

        public string Name => throw new NotImplementedException();

        private byte[] OnRequestKey(string message, int keyLength, out string error)
            => RequestMethods.RequestKey((args) => RequestData?.Invoke(this, args), message, keyLength, out error);

        private long OnRequestNumber(string message, long defaultValue, out string error)
            => RequestMethods.RequestNumber((args) => RequestData?.Invoke(this, args), message, defaultValue, out error);

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
            var key = OnRequestKey("AES256 XTS Key", 64, out var error);
            if (key == null)
                return Task.Factory.StartNew(() =>
                {
                    progress.Report(new ProgressReport { Percentage = 0, Message = error });
                    return false;
                });

            var sectorSize = OnRequestNumber("AES256 XTS SectorSize", 512, out error);
            if (!string.IsNullOrEmpty(error))
            {
                return Task.Factory.StartNew(() =>
                {
                    progress.Report(new ProgressReport
                    {
                        Percentage = 0,
                        Message = error
                    });
                    return false;
                });
            }

            return Task.Factory.StartNew(() =>
            {
                progress.Report(new ProgressReport { Percentage = 0, Message = decrypt ? "Decryption..." : "Encryption..." });

                using (var xts = new XtsStream(decrypt ? input : output, key, new byte[32], true, true, (int)sectorSize))
                {
                    var buffer = new byte[0x10000];
                    while (xts.Position < xts.Length)
                    {
                        var length = (int)Math.Min(0x10000, xts.Length - xts.Position);

                        if (decrypt)
                        {
                            xts.Read(buffer, 0, length);
                            output.Write(buffer, 0, length);
                        }
                        else
                        {
                            input.Read(buffer, 0, length);
                            xts.Write(buffer, 0, length);
                        }

                        progress.Report(new ProgressReport { Percentage = (double)xts.Length / xts.Position * 100, Message = decrypt ? "Decryption..." : "Encryption...", });
                    }
                }

                progress.Report(new ProgressReport { Percentage = 100, Message = decrypt ? "Decryption finished." : "Encryption finished." });

                return true;
            });
        }
    }
}
