using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace plugin_krypto_nintendo.Nca.Models
{
    public class NcaBodySection
    {
        /// <summary>
        /// The offset of the section in media units
        /// </summary>
        public long MediaOffset { get; }

        /// <summary>
        /// The length of the section in media units
        /// </summary>
        public long MediaLength { get; }

        /// <summary>
        /// The cipher context to use for the section
        /// </summary>
        public NcaSectionCrypto SectionCrypto { get; }
    }
}
