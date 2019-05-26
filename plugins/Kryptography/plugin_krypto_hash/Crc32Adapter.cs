using Kontract.Interfaces.Intermediate;
using Kontract.Models;
using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading.Tasks;
using Kontract.Interfaces;
using Kryptography.Hash.Crc;

namespace plugin_krypto_hash
{
    [Export(typeof(IPlugin))]
    public class Crc32Adapter : IHashAdapter
    {
        public string Name => "Crc32";
        public Task<HashResult> Compute(Stream toHash, IProgress<ProgressReport> progress)
        {
            return Task.Factory.StartNew(() =>
            {
                var crc = Crc32.Create(Crc32Formula.Normal);
                var hash = crc.Compute(toHash);

                var result = new byte[4];
                for (int i = 0; i < 4; i++)
                    result[i] = (byte)((hash >> (24 - i * 8)) & 0xFF);

                return new HashResult(true, result);
            });
        }
    }
}
