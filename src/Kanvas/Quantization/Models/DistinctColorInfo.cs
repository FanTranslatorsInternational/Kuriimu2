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

        public Color Color { get; }

        public float Hue { get; }

        public float Saturation { get; }

        public float Brightness { get; }

        public DistinctColorInfo(Color color)
        {
            Color = color;

            Hue = color.GetHue();
            Saturation = color.GetSaturation();
            Brightness = color.GetBrightness();

            Count = 1;
        }

        public DistinctColorInfo IncreaseCount()
        {
            Count++;
            return this;
        }
    }
}
