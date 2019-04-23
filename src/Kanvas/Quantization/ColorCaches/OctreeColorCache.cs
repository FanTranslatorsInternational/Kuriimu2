using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kanvas.Quantization.Helper;
using Kanvas.Quantization.Models.ColorCache;

namespace Kanvas.Quantization.ColorCaches
{
    public class OctreeColorCache : BaseColorCache
    {
        private OctreeCacheNode _root;

        protected override void OnPrepare()
        {
        }

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
                ColorModelHelper.GetSmallestEuclideanDistanceIndex(_colorModel, color, candidates.Values.ToList());

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
