using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Kanvas.Quantization.Helper;
using Kanvas.Quantization.Models.ColorCache;
using Kanvas.Support;

namespace Kanvas.Quantization.ColorCaches
{
    public class OctreeColorCache : BaseColorCache
    {
        private readonly OctreeCacheNode _root;

        public OctreeColorCache(IList<Color> palette) :
            base(palette)
        {
            _root = new OctreeCacheNode();

            Palette.ForEach((c, i) => _root.AddColor(c, i, 0));
        }

        /// <inheritdoc />
        protected override int OnGetPaletteIndex(Color color)
        {
            var candidates = _root.GetPaletteIndex(color, 0);

            var candidateColors = candidates.Values.ToArray();
            var colorIndex = EuclideanHelper.GetSmallestEuclideanDistanceIndex(candidateColors, color);

            return candidates.ElementAt(colorIndex).Key;
        }
    }
}
