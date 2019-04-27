using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kanvas.IndexEncoding.Models
{
    internal class AlphaIndexData : IndexData
    {
        public int Alpha { get; }

        public AlphaIndexData(int alpha, int index) : base(index)
        {
            Alpha = alpha;
        }
    }
}
