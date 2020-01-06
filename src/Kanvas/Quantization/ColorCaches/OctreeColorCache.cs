using System;
using System.Drawing;
using System.Linq;
using Kanvas.Quantization.Helper;
using Kanvas.Quantization.Models.ColorCache;

namespace Kanvas.Quantization.ColorCaches
{
    public class OctreeColorCache : BaseColorCache
    {
        private OctreeCacheNode _root;

        protected override void OnCachePalette()
        {
            _root = new OctreeCacheNode();

            Int32 index = 0;
            foreach (Color color in Palette)
            {
                _root.AddColor(color, index++, 0);
            }
        }

        protected override int CalculatePaletteIndex(Color color)
        {
            var candidates = _root.GetPaletteIndex(color, 0);

            var result = 0;
            int index = 0;
            int colorIndex =
                ColorModelHelper.GetSmallestEuclideanDistanceIndex(ColorModel, color, candidates.Values.ToList(), AlphaThreshold);

            foreach (var colorPaletteIndex in candidates.Keys)
            {
                if (index == colorIndex)
                {
                    result = colorPaletteIndex;
                    break;
                }

                index++;
            }

            return result;
        }
    }
}
