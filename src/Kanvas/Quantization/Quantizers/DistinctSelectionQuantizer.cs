using Kanvas.Quantization.Helper;
using Kanvas.Quantization.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Kanvas.Quantization.Models.ColorCache;
using Kanvas.Quantization.Models.Parallel;
using Kanvas.Quantization.Models.Quantizer;
using Kanvas.Support;

namespace Kanvas.Quantization.Quantizers
{
    /// <inheritdoc cref="IColorQuantizer"/>
    public class DistinctSelectionQuantizer : IColorQuantizer
    {
        private int _colorCount;
        private IColorCache _colorCache;
        private ConcurrentDictionary<uint, DistinctColorInfo> _distinctColors;

        #region IColorQuantizer

        /// <inheritdoc cref="IColorQuantizer.UsesColorCache"/>
        public bool UsesColorCache => true;

        /// <inheritdoc cref="IColorQuantizer.UsesVariableColorCount"/>
        public bool UsesVariableColorCount => true;

        /// <inheritdoc cref="IColorQuantizer.AllowParallel"/>
        public bool AllowParallel => true;

        public int TaskCount { get; private set; } = 8;

        /// <inheritdoc cref="IColorQuantizer.SetColorCache(IColorCache)"/>
        public void SetColorCache(IColorCache colorCache)
        {
            _colorCache = colorCache;
        }

        /// <inheritdoc cref="IColorQuantizer.SetColorCount(int)"/>
        public void SetColorCount(int colorCount)
        {
            _colorCount = colorCount;
        }

        /// <inheritdoc cref="IColorQuantizer.SetParallelTasks(int)"/>
        public void SetParallelTasks(int taskCount)
        {
            TaskCount = taskCount;
        }

        public IEnumerable<int> Process(IEnumerable<Color> colors)
        {
            var colorArray = colors.ToArray();

            // Step 1: Create and cache palette
            CreatePalette(colorArray);

            // Step 2: Loop through original colors and get nearest match from cache
            var indices = GetIndeces(colorArray);

            return indices;
        }

        public int GetPaletteIndex(Color color)
        {
            return _colorCache.GetPaletteIndex(color);
        }

        public IList<Color> GetPalette()
        {
            return _colorCache.Palette;
        }

        #endregion

        private void FillDistinctColors(Color[] colors)
        {
            _distinctColors = new ConcurrentDictionary<uint, DistinctColorInfo>();

            void ProcessingAction(LineTask<Color[], ConcurrentDictionary<uint, DistinctColorInfo>> taskModel)
            {
                for (int i = taskModel.Start; i < taskModel.Start + taskModel.Length; i++)
                {
                    var color = taskModel.Input[i];
                    if (_colorCache.ColorModel == ColorModel.RGBA)
                        color = color.A >= _colorCache.AlphaThreshold ? Color.FromArgb(255, color.R, color.G, color.B) : Color.Transparent;
                    taskModel.Output.AddOrUpdate((uint)color.ToArgb(), key => new DistinctColorInfo(color),
                        (key, info) => info.IncreaseCount());
                }
            }

            ParallelProcessing.ProcessList(colors, _distinctColors, ProcessingAction, TaskCount);
        }

        public void CreatePalette(IEnumerable<Color> colors)
        {
            // Step 1: Filter out distinct colors
            FillDistinctColors(colors.ToArray());

            // Step 2: Filter colors by hue, saturation and brightness
            // Step 2.1: If color count not reached, take top(n) colors
            var palette = FilterColorInfos();

            // Step 3: Cache palette
            _colorCache.CachePalette(palette);
        }

        private List<Color> FilterColorInfos()
        {
            var colorInfoList = _distinctColors.Values.ToList();
            var foundColorCount = colorInfoList.Count;
            var maxColorCount = _colorCount;

            if (foundColorCount < maxColorCount)
                return colorInfoList.Select(info => Color.FromArgb(info.Color)).ToList();

            var random = new FastRandom(13);
            colorInfoList = colorInfoList.
                OrderBy(info => random.Next(foundColorCount)).
                ToList();

            bool usedTransparency = false;
            if (_colorCache.ColorModel == ColorModel.RGBA)
                if (colorInfoList.Exists(x => x.Color == Color.Transparent.ToArgb()))
                {
                    colorInfoList.RemoveAll(x => x.Color == Color.Transparent.ToArgb());
                    usedTransparency = true;
                    maxColorCount--;
                }

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
            if (usedTransparency)
                palette.Add(Color.Transparent);
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

        private IEnumerable<int> GetIndeces(IEnumerable<Color> colors)
        {
            var colorList = colors.ToArray();
            var indices = new int[colorList.Length];

            void ProcessingAction(LineTask<Color[], int[]> taskModel)
            {
                for (int i = taskModel.Start; i < taskModel.Start + taskModel.Length; i++)
                    taskModel.Output[i] = _colorCache.GetPaletteIndex(taskModel.Input[i]);
            }

            ParallelProcessing.ProcessList(colorList, indices, ProcessingAction, TaskCount);

            return indices;
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
