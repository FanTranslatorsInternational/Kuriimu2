using Kanvas.Quantization.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kanvas.Quantization.Models.ColorCache;

namespace Kanvas.Quantization.Helper
{
    class ColorModelHelper
    {
        public static ColorModelComponents GetColorComponents(ColorModel model, Color color)
        {
            switch (model)
            {
                case ColorModel.RGB:
                    return new ColorModelComponents(color.R, color.G, color.B);
                case ColorModel.RGBA:
                    return new ColorModelComponents(color.R, color.G, color.B) { ComponentD = color.A };
                default:
                    throw new InvalidOperationException($"ColorModel {model} not supported.");
            }
        }

        public static int GetSmallestEuclideanDistanceIndex(ColorModel model, Color sourceColor, IList<Color> colorList, int alphaThreshold)
        {
            long leastDistance = long.MaxValue;
            int result = 0;
            for (int i = 0; i < colorList.Count; i++)
            {
                var distance = GetEuclideanDistance(GetColorComponentsDistance(model, sourceColor, colorList[i]));
                if (distance == 0)
                    return i;

                if (distance < leastDistance)
                {
                    leastDistance = distance;
                    result = i;
                }
            }

            return result;
        }

        public static long GetEuclideanDistance(ColorModelComponents distance /*ColorModel model, Color requestedColor, Color targetColor*/)
        {
            // var comps = GetColorComponentsDistance(model, requestedColor, targetColor);
            var result = distance.ComponentA * distance.ComponentA + distance.ComponentB * distance.ComponentB +
                           distance.ComponentC * distance.ComponentC;
            if (distance.ComponentD.HasValue) result += distance.ComponentD.Value * distance.ComponentD.Value;
            return (long)result;
        }

        public static ColorModelComponents GetColorComponentsDistance(ColorModel model, Color sourceColor, Color targetColor)
        {
            var sourceComponents = GetColorComponents(model, sourceColor);
            var targetComponents = GetColorComponents(model, targetColor);

            var distance = new ColorModelComponents(
                sourceComponents.ComponentA - targetComponents.ComponentA,
                sourceComponents.ComponentB - targetComponents.ComponentB,
                sourceComponents.ComponentC - targetComponents.ComponentC);
            if (sourceComponents.ComponentD.HasValue && targetComponents.ComponentD.HasValue)
                distance.ComponentD = sourceComponents.ComponentD - targetComponents.ComponentD;

            return distance;
        }
    }
}
