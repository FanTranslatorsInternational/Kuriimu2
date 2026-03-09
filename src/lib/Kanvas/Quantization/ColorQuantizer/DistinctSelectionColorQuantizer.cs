using System.Collections.Concurrent;
using Kanvas.Contract.Quantization.ColorCache;
using Kanvas.Contract.Quantization.ColorQuantizer;
using Kanvas.DataClasses.Quantization.Quantizer.DistinctSelection;
using SixLabors.ImageSharp.PixelFormats;

namespace Kanvas.Quantization.ColorQuantizer
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
        public IList<Rgba32> CreatePalette(IEnumerable<Rgba32> colors)
        {
            return CreatePalette(colors, []);
        }

        public IList<Rgba32> CreatePalette(IEnumerable<Rgba32> colors, IList<Rgba32> initialPalette)
        {
            var fixedPalette = NormalizeInitialPalette(initialPalette, _colorCount);

            int remainingColorCount = _colorCount - fixedPalette.Count;
            if (remainingColorCount <= 0)
                return fixedPalette;

            // Step 1: Filter out distinct colors
            var distinctColors = FillDistinctColors(colors.ToArray(), fixedPalette);

            // Step 2: Filter colors by hue, saturation and brightness
            // Step 2.1: If color count not reached, take top(n) colors
            var palette = FilterColorInfos(distinctColors, remainingColorCount);

            // Step 3: Return palette
            fixedPalette.AddRange(palette);

            return fixedPalette;
        }

        /// <inheritdoc />
        public IColorCache GetFixedColorCache(IList<Rgba32> palette)
        {
            throw new NotSupportedException();
        }

        private IDictionary<uint, DistinctColorInfo> FillDistinctColors(IList<Rgba32> colors, IList<Rgba32> initialPalette)
        {
            var distinctColors = new ConcurrentDictionary<uint, DistinctColorInfo>();
            var initialColorSet = new HashSet<uint>(initialPalette.Select(c => c.PackedValue));

            colors.AsParallel()
                .WithDegreeOfParallelism(_taskCount)
                .ForAll(c => AddOrUpdateDistinctColors(distinctColors, c, initialColorSet));

            return distinctColors;
        }

        private void AddOrUpdateDistinctColors(ConcurrentDictionary<uint, DistinctColorInfo> distinctColors, Rgba32 color, HashSet<uint> initialPaletteSet)
        {
            if (initialPaletteSet.Contains(color.PackedValue))
                return;

            distinctColors.AddOrUpdate(color.PackedValue,
                _ => new DistinctColorInfo(color),
                (_, info) => info.IncreaseCount());
        }

        private List<Rgba32> FilterColorInfos(IDictionary<uint, DistinctColorInfo> distinctColors, int colorCount)
        {
            var colorInfoList = distinctColors.Values.ToList();
            var foundColorCount = colorInfoList.Count;
            var maxColorCount = colorCount;

            if (foundColorCount < maxColorCount)
                return colorInfoList.Select(info => new Rgba32(info.Color)).ToList();

            var random = new DistinctSelection.FastRandom(13);
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
                out colorInfoList))
            {
            }

            int listColorCount = colorInfoList.Count;

            if (listColorCount > 0)
            {
                int allowedTake = Math.Min(maxColorCount, listColorCount);
                colorInfoList = colorInfoList.Take(allowedTake).ToList();
            }

            var palette = new List<Rgba32>
            {
                new(background.Color)
            };
            palette.AddRange(colorInfoList.Select(colorInfo => new Rgba32(colorInfo.Color)));

            return palette;
        }

        private static List<Rgba32> NormalizeInitialPalette(IList<Rgba32> initialPalette, int maxColorCount)
        {
            if (maxColorCount <= 0 || initialPalette.Count <= 0)
                return [];

            return initialPalette
                .DistinctBy(color => color.PackedValue)
                .Take(maxColorCount)
                .ToList();
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
