using Kontract;
using Kontract.Attributes;
using Kontract.Interfaces.Common;
using Kontract.Interfaces.Intermediate;
using plugin_krypto_nintendo.Nca;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace plugin_krypto_nintendo.Adapters
{
    [Export(typeof(IPlugin))]
    [MenuStripExtension("Nintendo", "Nca")]
    public class NcaAdapter : ICipherAdapter
    {
        public string Name => "Nca";

        public event EventHandler<RequestDataEventArgs> RequestData;

        private string OnRequestFile(string message, out string error)
            => RequestMethods.RequestFile((args) => RequestData?.Invoke(this, args), message, out error);

        public Task<bool> Decrypt(Stream toDecrypt, Stream decryptInto, IProgress<ProgressReport> progress)
        {
            var keyFile = OnRequestFile("Select Switch key file...", out var error);
            if (!string.IsNullOrEmpty(error))
            {
                return Task.Factory.StartNew(() =>
                {
                    progress.Report(new ProgressReport { Percentage = 0, Message = error });
                    return false;
                });
            }

            var factory = new NcaFactory(toDecrypt, keyFile);
            if (factory.HasRightsId)
            {
                var titleKeyFile = OnRequestFile("Select Switch title key file...", out error);
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

        // TODO: Implement encryption
        public Task<bool> Encrypt(Stream toEncrypt, Stream encryptInto, IProgress<ProgressReport> progress)
        {
            throw new NotImplementedException();
        }
    }
}
