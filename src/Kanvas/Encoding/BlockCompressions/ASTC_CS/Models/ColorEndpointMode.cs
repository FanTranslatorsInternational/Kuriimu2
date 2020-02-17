using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Komponent.IO;

namespace Kanvas.Encoding.BlockCompressions.ASTC_CS.Models
{
    class ColorEndpointMode
    {
        public int Class { get; }

        public int EndpointValueCount { get; }

        public static ColorEndpointMode Create(BitReader br)
        {
            return new ColorEndpointMode(br.ReadBits<int>(4));
        }

        private ColorEndpointMode(int value)
        {
            Class = value / 4;
            EndpointValueCount = (Class + 1) * 2;
        }
    }
}
