using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kanvas.Encoding.BlockCompressions.BCn.Models
{
    public class Bc4BlockData
    {
        public float MinValue { get; set; }

        public float MaxValue { get; set; }

        public float[] InterpretedValues { get; set; } = new float[8];

        public float[] Values { get; set; } = new float[16];
    }
}
