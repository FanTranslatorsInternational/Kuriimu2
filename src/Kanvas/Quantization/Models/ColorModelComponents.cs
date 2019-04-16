using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kanvas.Quantization.Models
{
    class ColorModelComponents
    {
        public float ComponentA { get; }
        public float ComponentB { get; }
        public float ComponentC { get; }
        public float? ComponentD { get; set; }

        public ColorModelComponents(float compA, float compB, float compC)
        {
            ComponentA = compA;
            ComponentB = compB;
            ComponentC = compC;
        }
    }
}
