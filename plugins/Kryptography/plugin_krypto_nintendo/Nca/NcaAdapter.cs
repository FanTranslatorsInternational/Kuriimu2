using Kontract.Attributes;
using Kontract.Interfaces.Intermediate;
using plugin_krypto_nintendo.Nca.Factories;
using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading.Tasks;
using Kontract.Interfaces;
using Kontract.Models;
using Kontract.Models.Intermediate;

namespace plugin_krypto_nintendo.Nca
{
    [Export(typeof(IPlugin))]
    [MenuStripExtension("Nintendo", "Nca")]
    public class NcaAdapter : ICipherAdapter
    {
        public string Name => "Nca";

        public event EventHandler<RequestDataEventArgs> RequestData;

        private string OnRequestFile(string message, string requestId, out string error)
            => RequestMethods.RequestFile((args) => RequestData?.Invoke(this, args), message, requestId, out error);

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

            var keyFile = OnRequestFile("Select Switch key file...", requestId, out var error);
            if (!string.IsNullOrEmpty(error))
            {
                return Task.Factory.StartNew(() =>
                {
                    progress.Report(new ProgressReport { Percentage = 0, Message = error });
                    return false;
                });
            }
            
            var factory = new NcaFactory(input, keyFile);
            if (factory.UseTitleKeyEncryption)
            {
                var titleKeyFile = OnRequestFile("Select Switch title key file...", requestId, out error);
                if (!string.IsNullOrEmpty(error))
                {
                    return Task.Factory.StartNew(() =>
                    {
                        progress.Report(new ProgressReport { Percentage = 0, Message = error });
                        return false;
                    });
                }

                factory.SetTitleKeyFile(titleKeyFile);
            }
            
            return Task.Factory.StartNew(() =>
            {
                progress.Report(new ProgressReport { Percentage = 0, Message = "Decryption..." });

                using (var cs = factory.CreateStream(decrypt ? input : output))
                {
                    var buffer = new byte[0x10000];
                    var totalLength = decrypt ? cs.Length : input.Length;
                    while (cs.Position < totalLength)
                    {
                        var length = (int)Math.Min(0x10000, totalLength - cs.Position);

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

                        progress.Report(new ProgressReport { Percentage = (double)cs.Position / totalLength * 100, Message = "Decryption..." });
                    }
                }

                progress.Report(new ProgressReport { Percentage = 100, Message = "Decryption finished." });

                return true;
            });
        }
    }
}
