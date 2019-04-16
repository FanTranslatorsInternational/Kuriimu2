using Kanvas.Quantization.ColorCaches;
using Kanvas.Quantization.Interfaces;
using Kanvas.Quantization.Models;
using MoreLinq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kanvas.Quantization.Quantizers
{
    /// <inheritdoc cref="IColorQuantizer"/>
    public class DistinctSelectionQuantizer : IColorQuantizer
    {
        private readonly int _colorCount;
        private readonly IColorCache _colorCache;
        private ConcurrentDictionary<int, DistinctColorInfo> _distinctColors;

        /// <summary>
        /// Creates a new instance of <see cref="DistinctSelectionQuantizer"/>
        /// </summary>
        /// <param name="colorCount">The maximum count of colors in the final palette.</param>
        public DistinctSelectionQuantizer(int colorCount)
        {
            _colorCount = colorCount;
            _colorCache = new EuclideanDistanceColorCache(ColorModel.RGB);
        }

        /// <summary>
        /// Creates a new instance of <see cref="DistinctSelectionQuantizer"/>
        /// </summary>
        /// <param name="colorCount">The maximum count of colors in the final palette.</param>
        /// <param name="colorCache">The cache implementation to use.</param>
        public DistinctSelectionQuantizer(int colorCount, IColorCache colorCache)
        {
            _colorCount = colorCount;
            _colorCache = colorCache;
        }

        /// <inheritdoc cref="IColorQuantizer.Process"/>
        public (IEnumerable<int> indeces, IList<Color> palette) Process(Bitmap image)
        {
            // Step 1: Get all distinct colors from the image
            FillDistinctColors(image);

            // Step 2: Filter colors by hue, saturation and brightness
            // Step 2.1: If color count not reached, take top(n) colors
            var palette = FilterColorInfos();

            // Step 3: Cache filtered colors
            _colorCache.CachePalette(palette);

            // Step 4: Loop through original colors and get nearest match from cache
            var indeces = GetIndeces(image);

            return (indeces, palette);
        }

        private void FillDistinctColors(Bitmap image)
        {
            _distinctColors = new ConcurrentDictionary<int, DistinctColorInfo>();

            for (int y = 0; y < image.Height; y++)
                for (int x = 0; x < image.Width; x++)
                {
                    var color = image.GetPixel(x, y);
                    _distinctColors.AddOrUpdate(color.ToArgb(), key => new DistinctColorInfo(color),
                        (key, info) => info.IncreaseCount());
                }
        }

        private List<Color> FilterColorInfos()
        {
            var colorInfoList = _distinctColors.Values.ToList();
            var foundColorCount = colorInfoList.Count;
            var maxColorCount = _colorCount;

            if (foundColorCount < maxColorCount)
                return colorInfoList.Select(info => Color.FromArgb(info.Color)).ToList();

            var random = new Random(13);
            colorInfoList = colorInfoList.OrderBy(info => random.Next(foundColorCount)).ToList();

            DistinctColorInfo background = colorInfoList.MaxBy(info => info.Count);
            colorInfoList.Remove(background);
            maxColorCount--;

            // Filter by hue, saturation and brightness
            var comparers = new List<IEqualityComparer<DistinctColorInfo>> { new ColorHueComparer(), new ColorSaturationComparer(), new ColorBrightnessComparer() };
            while (ProcessList(maxColorCount, colorInfoList, comparers,
                out colorInfoList))
            {
            }

            int listColorCount = colorInfoList.Count;

            if (listColorCount > 0)
            {
                int allowedTake = Math.Min(maxColorCount, listColorCount);
                colorInfoList = colorInfoList.Take(allowedTake).ToList();
            }
            
            var palette = new List<Color>
            {
                Color.FromArgb(background.Color)
            };
            palette.AddRange(colorInfoList.Select(colorInfo => Color.FromArgb(colorInfo.Color)));
            return palette;
        }

        private static bool ProcessList(int colorCount, List<DistinctColorInfo> list, ICollection<IEqualityComparer<DistinctColorInfo>> comparers, out List<DistinctColorInfo> outputList)
        {
            IEqualityComparer<DistinctColorInfo> bestComparer = null;
            Int32 maximalCount = 0;
            outputList = list;

            foreach (IEqualityComparer<DistinctColorInfo> comparer in comparers)
            {
                List<DistinctColorInfo> filteredList = list.
                    Distinct(comparer).
                    ToList();

                Int32 filteredListCount = filteredList.Count;

                if (filteredListCount > colorCount && filteredListCount > maximalCount)
                {
                    maximalCount = filteredListCount;
                    bestComparer = comparer;
                    outputList = filteredList;

                    if (maximalCount <= colorCount) break;
                }
            }

            comparers.Remove(bestComparer);
            return comparers.Count > 0 && maximalCount > colorCount;
        }

        // TODO: Getting indeces can be parallelized
        private IEnumerable<int> GetIndeces(Bitmap image)
        {
            for (int y = 0; y < image.Height; y++)
                for (int x = 0; x < image.Width; x++)
                {
                    yield return _colorCache.GetPaletteIndex(image.GetPixel(x, y));
                }
        }

        #region Equality Comparers

        /// <summary>
        /// Compares a hue components of a color info.
        /// </summary>
        private class ColorHueComparer : IEqualityComparer<DistinctColorInfo>
        {
            public Boolean Equals(DistinctColorInfo x, DistinctColorInfo y)
            {
                return x.Hue == y.Hue;
            }

            public Int32 GetHashCode(DistinctColorInfo colorInfo)
            {
                return colorInfo.Hue.GetHashCode();
            }
        }

        /// <summary>
        /// Compares a saturation components of a color info.
        /// </summary>
        private class ColorSaturationComparer : IEqualityComparer<DistinctColorInfo>
        {
            public Boolean Equals(DistinctColorInfo x, DistinctColorInfo y)
            {
                return x.Saturation == y.Saturation;
            }

            public Int32 GetHashCode(DistinctColorInfo colorInfo)
            {
                return colorInfo.Saturation.GetHashCode();
            }
        }

        /// <summary>
        /// Compares a brightness components of a color info.
        /// </summary>
        private class ColorBrightnessComparer : IEqualityComparer<DistinctColorInfo>
        {
            public Boolean Equals(DistinctColorInfo x, DistinctColorInfo y)
            {
                return x.Brightness == y.Brightness;
            }

            public Int32 GetHashCode(DistinctColorInfo colorInfo)
            {
                return colorInfo.Brightness.GetHashCode();
            }
        }

        #endregion
    }
}
