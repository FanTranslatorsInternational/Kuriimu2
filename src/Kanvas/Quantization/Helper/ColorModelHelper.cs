using Kanvas.Quantization.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kanvas.Quantization.Helper
{
    class ColorModelHelper
    {
        public static long GetEuclideanDistance(ColorModel model, Color requestedColor, Color targetColor)
        {
            var comps = GetColorComponentsDistance(model, requestedColor, targetColor);
            var distance = comps.ComponentA * comps.ComponentA + comps.ComponentB * comps.ComponentB +
                           comps.ComponentC * comps.ComponentC;
            if (comps.ComponentD.HasValue) distance += comps.ComponentD.Value * comps.ComponentD.Value;
            return (long) distance;
        }

        private static ColorModelComponents GetColorComponentsDistance(ColorModel model, Color color, Color targetColor)
        {
            switch (model)
            {
                case ColorModel.RGB:
                    return new ColorModelComponents(color.R - targetColor.R, color.G - targetColor.G, color.B - targetColor.B);
                case ColorModel.RGBA:
                    return new ColorModelComponents(color.R - targetColor.R, color.G - targetColor.G,
                        color.B - targetColor.B)
                    {
                        ComponentD = color.A - targetColor.A
                    };
                default:
                    throw new InvalidOperationException($"ColorModel {model} is not supported.");
            }
        }
    }
}
