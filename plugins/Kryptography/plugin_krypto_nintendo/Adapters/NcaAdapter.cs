using Kontract;
using Kontract.Attributes;
using Kontract.Interfaces.Intermediate;
using plugin_krypto_nintendo.Nca;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace plugin_krypto_nintendo.Adapters
{
    [MenuStripExtension("Nintendo", "Nca")]
    public class NcaAdapter : ICipherAdapter
    {
        public string Name => "Nca";

        public event EventHandler<RequestKeyEventArgs> RequestKey;

        public Task<bool> Decrypt(Stream toDecrypt, Stream decryptInto, IProgress<ProgressReport> progress)
        {
            // TODO: Get keyfile path
            var factory = new NcaFactory(toDecrypt, "");
            if (factory.HasRightsId)
                // TODO: Get titlekey file
                factory.SetTitleKeyFile("");

            return Task.Factory.StartNew(() =>
            {
                progress.Report(new ProgressReport { Percentage = 0, Message = "Decryption..." });

                using (var cs = factory.CreateReadableStream())
                {
                    var buffer = new byte[0x10000];
                    while (cs.Position < cs.Length)
                    {
                        var length = (int)Math.Min(0x10000, cs.Length - cs.Position);

                        cs.Read(buffer, 0, length);
                        decryptInto.Write(buffer, 0, length);

                        progress.Report(new ProgressReport { Percentage = (double)cs.Length / cs.Position * 100, Message = "Decryption..." });
                    }
                }

                progress.Report(new ProgressReport { Percentage = 100, Message = "Decryption finished." });

                return true;
            });
        }

        public Task<bool> Encrypt(Stream toEncrypt, Stream encryptInto, IProgress<ProgressReport> progress)
        {
            throw new NotImplementedException();
        }
    }
}
