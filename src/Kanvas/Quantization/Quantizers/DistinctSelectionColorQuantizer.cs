using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Kanvas.MoreEnumerable;
using Kanvas.Quantization.Models.Quantizer.DistinctSelection;
using Kontract.Kanvas.Quantization;

namespace Kanvas.Quantization.Quantizers
{
    /// <inheritdoc cref="IColorQuantizer"/>
    public class DistinctSelectionColorQuantizer : IColorQuantizer
    {
        private readonly int _colorCount;
        private readonly int _taskCount;

        /// <inheritdoc />
        public bool IsColorCacheFixed => false;

        /// <inheritdoc />
        public bool UsesVariableColorCount => true;

        /// <inheritdoc />
        public bool SupportsAlpha => false;

        public DistinctSelectionColorQuantizer(int colorCount, int taskCount)
        {
            _colorCount = colorCount;
            _taskCount = taskCount;
        }

        /// <inheritdoc />
        public IList<Color> CreatePalette(IEnumerable<Color> colors)
        {
            // Step 1: Filter out distinct colors
            var distinctColors = FillDistinctColors(colors.ToArray());

            // Step 2: Filter colors by hue, saturation and brightness
            // Step 2.1: If color count not reached, take top(n) colors
            var palette = FilterColorInfos(distinctColors);

            // Step 3: Return palette
            return palette;
        }

        /// <inheritdoc />
        public IColorCache GetFixedColorCache(IList<Color> palette)
        {
            throw new NotSupportedException();
        }

        private IDictionary<uint, DistinctColorInfo> FillDistinctColors(IList<Color> colors)
        {
            var distinctColors = new ConcurrentDictionary<uint, DistinctColorInfo>();

            colors.AsParallel()
                .WithDegreeOfParallelism(_taskCount)
                .ForAll(c => AddOrUpdateDistinctColors(distinctColors, c));

            return distinctColors;
        }

        private void AddOrUpdateDistinctColors(ConcurrentDictionary<uint, DistinctColorInfo> distinctColors, Color color)
        {
            distinctColors.AddOrUpdate((uint)color.ToArgb(),
                key => new DistinctColorInfo(color),
                (key, info) => info.IncreaseCount());
        }

        private List<Color> FilterColorInfos(IDictionary<uint, DistinctColorInfo> distinctColors)
        {
            var colorInfoList = distinctColors.Values.ToList();
            var foundColorCount = colorInfoList.Count;
            var maxColorCount = _colorCount;

            if (foundColorCount < maxColorCount)
                return colorInfoList.Select(info => Color.FromArgb(info.Color)).ToList();

            var random = new FastRandom(13);
            colorInfoList = colorInfoList.
                OrderBy(info => random.Next(foundColorCount)).
                ToList();

            var background = colorInfoList.MaxBy(info => info.Count);
            colorInfoList.Remove(background);
            maxColorCount--;

            // Filter by hue, saturation and brightness
            var comparers = new List<IEqualityComparer<DistinctColorInfo>>
            {
                new ColorHueComparer(),
                new ColorSaturationComparer(),
                new ColorBrightnessComparer()
            };

            while (ProcessList(maxColorCount, colorInfoList, comparers,
                out colorInfoList)) { }

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
