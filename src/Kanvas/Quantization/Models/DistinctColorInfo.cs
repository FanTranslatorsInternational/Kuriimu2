using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kanvas.Quantization.Models
{
    class DistinctColorInfo
    {
        public int Count { get; private set; }

        public int Color { get; }

        public int Hue { get; }

        public int Saturation { get; }

        public int Brightness { get; }

        public DistinctColorInfo(Color color)
        {
            Color = color.ToArgb();

            Hue = Convert.ToInt32(color.GetHue() * 5000000);
            Saturation = Convert.ToInt32(color.GetSaturation() * 5000000);
            Brightness = Convert.ToInt32(color.GetBrightness() * 5000000);

            Count = 1;
        }

        public DistinctColorInfo IncreaseCount()
        {
            Count++;
            return this;
        }
    }
}
