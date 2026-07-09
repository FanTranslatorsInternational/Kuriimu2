using System.Collections.Concurrent;
using Kanvas.Contract.Quantization.ColorCache;
using Kanvas.Contract.Quantization.ColorQuantizer;
using Kanvas.DataClasses.Quantization.Quantizer.DistinctSelection;
using SixLabors.ImageSharp.PixelFormats;

namespace Kanvas.Quantization.ColorQuantizer
{
    /// <inheritdoc cref="IColorQuantizer"/>
    public class DistinctSelectionColorQuantizer(int colorCount, int taskCount) : IColorQuantizer
    {
        /// <inheritdoc />
        public bool IsColorCacheFixed => false;

        /// <inheritdoc />
        public bool UsesVariableColorCount => true;

        /// <inheritdoc />
        public bool SupportsAlpha => true;

        /// <inheritdoc />
        public IList<Rgba32> CreatePalette(IList<Rgba32> colors)
        {
            return CreatePalette(colors, []);
        }

        public IList<Rgba32> CreatePalette(IList<Rgba32> colors, IList<Rgba32> initialPalette)
        {
            var fixedPalette = NormalizeInitialPalette(initialPalette, colorCount);

            int remainingColorCount = colorCount - fixedPalette.Count;
            if (remainingColorCount <= 0)
                return fixedPalette;

            // Step 1: Filter out distinct colors
            var distinctColors = FillDistinctColors(colors, fixedPalette);

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

        private ConcurrentDictionary<uint, DistinctColorInfo> FillDistinctColors(IList<Rgba32> colors, IList<Rgba32> initialPalette)
        {
            var distinctColors = new ConcurrentDictionary<uint, DistinctColorInfo>();
            var initialColorSet = new HashSet<uint>(initialPalette.Select(c => c.PackedValue));

            colors.AsParallel()
                .WithDegreeOfParallelism(taskCount)
                .ForAll(c => AddOrUpdateDistinctColors(distinctColors, c, initialColorSet));

            return distinctColors;
        }

        private static void AddOrUpdateDistinctColors(ConcurrentDictionary<uint, DistinctColorInfo> distinctColors, Rgba32 color, HashSet<uint> initialPaletteSet)
        {
            if (initialPaletteSet.Contains(color.PackedValue))
                return;

            var normalizedColor = NormalizeTransparentColor(color);
            distinctColors.AddOrUpdate(normalizedColor.PackedValue,
                _ => new DistinctColorInfo(normalizedColor),
                (_, info) => info.IncreaseCount());
        }

        private static List<Rgba32> FilterColorInfos(ConcurrentDictionary<uint, DistinctColorInfo> distinctColors, int maxColorCount)
        {
            var colorInfoList = distinctColors.Values.ToList();
            var foundColorCount = colorInfoList.Count;

            if (foundColorCount < maxColorCount)
                return [.. colorInfoList.Select(info => new Rgba32(info.Color))];

            var random = new DistinctSelection.FastRandom(13);
            colorInfoList = [.. colorInfoList.OrderBy(_ => random.Next(foundColorCount))];

            var background = colorInfoList.MaxBy(info => info.Count)!;
            colorInfoList.Remove(background);
            maxColorCount--;

            // Filter by hue, saturation and brightness
            var comparers = new List<IEqualityComparer<DistinctColorInfo>>
            {
                new ColorAlphaComparer(),
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
                colorInfoList = [.. colorInfoList.Take(allowedTake)];
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

            return [.. initialPalette.DistinctBy(color => color.PackedValue).Take(maxColorCount)];
        }

        private static Rgba32 NormalizeTransparentColor(Rgba32 color)
        {
            return color.A == 0 ? default : color;
        }

        private static bool ProcessList(int colorCount, List<DistinctColorInfo> list, List<IEqualityComparer<DistinctColorInfo>> comparers, out List<DistinctColorInfo> outputList)
        {
            IEqualityComparer<DistinctColorInfo>? bestComparer = null;

            var maximalCount = 0;
            outputList = list;

            foreach (IEqualityComparer<DistinctColorInfo> comparer in comparers)
            {
                List<DistinctColorInfo> filteredList = [.. list.Distinct(comparer)];

                int filteredListCount = filteredList.Count;

                if (filteredListCount > colorCount && filteredListCount > maximalCount)
                {
                    maximalCount = filteredListCount;
                    bestComparer = comparer;
                    outputList = filteredList;

                    if (maximalCount <= colorCount) break;
                }
            }

            if (bestComparer != null)
                comparers.Remove(bestComparer);

            return comparers.Count > 0 && maximalCount > colorCount;
        }

        #region Equality Comparers

        /// <summary>
        /// Compares alpha components of a color info.
        /// </summary>
        private class ColorAlphaComparer : IEqualityComparer<DistinctColorInfo>
        {
            private const int AlphaBucketShift = 0;

            public bool Equals(DistinctColorInfo? x, DistinctColorInfo? y)
            {
                return GetAlphaBucket(x) == GetAlphaBucket(y);
            }

            public int GetHashCode(DistinctColorInfo colorInfo)
            {
                return GetAlphaBucket(colorInfo);
            }

            private static int GetAlphaBucket(DistinctColorInfo? colorInfo)
            {
                if (colorInfo is null)
                    return 0;

                return colorInfo.Alpha >> AlphaBucketShift;
            }
        }

        /// <summary>
        /// Compares a hue components of a color info.
        /// </summary>
        private class ColorHueComparer : IEqualityComparer<DistinctColorInfo>
        {
            public bool Equals(DistinctColorInfo? x, DistinctColorInfo? y)
            {
                return x?.Hue == y?.Hue;
            }

            public int GetHashCode(DistinctColorInfo colorInfo)
            {
                return colorInfo.Hue.GetHashCode();
            }
        }

        /// <summary>
        /// Compares a saturation components of a color info.
        /// </summary>
        private class ColorSaturationComparer : IEqualityComparer<DistinctColorInfo>
        {
            public bool Equals(DistinctColorInfo? x, DistinctColorInfo? y)
            {
                return x?.Saturation == y?.Saturation;
            }

            public int GetHashCode(DistinctColorInfo colorInfo)
            {
                return colorInfo.Saturation.GetHashCode();
            }
        }

        /// <summary>
        /// Compares a brightness components of a color info.
        /// </summary>
        private class ColorBrightnessComparer : IEqualityComparer<DistinctColorInfo>
        {
            public bool Equals(DistinctColorInfo? x, DistinctColorInfo? y)
            {
                return x?.Brightness == y?.Brightness;
            }

            public int GetHashCode(DistinctColorInfo colorInfo)
            {
                return colorInfo.Brightness.GetHashCode();
            }
        }

        #endregion
    }
}
