using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace plugin_krypto_nintendo.Nca.Models
{
    public class NcaBodySection
    {
        public long Offset { get; }
        public long Length { get; }
        public NcaSectionCrypto SectionCrypto { get; }
    }
}
