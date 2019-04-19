using Kanvas.Quantization.ColorCaches;
using Kanvas.Quantization.Helper;
using Kanvas.Quantization.Interfaces;
using Kanvas.Quantization.Models;
using MoreLinq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
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
        private int _colorCount;
        private IColorCache _colorCache;
        private int _taskCount = 8;
        private ConcurrentDictionary<int, DistinctColorInfo> _distinctColors;

        #region IColorQuantizer

        /// <inheritdoc cref="IColorQuantizer.UsesColorCache"/>
        public bool UsesColorCache => true;

        /// <inheritdoc cref="IColorQuantizer.UsesVariableColorCount"/>
        public bool UsesVariableColorCount => true;

        /// <inheritdoc cref="IColorQuantizer.AllowParallel"/>
        public bool AllowParallel => true;

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
            _taskCount = taskCount;
        }

        public IEnumerable<int> Process(IEnumerable<Color> colors)
        {
            // Step 1: Get all distinct colors from the image
            FillDistinctColors(colors);

            // Step 2: Create palette
            CreateAndCachePalette();

            // Step 3: Loop through original colors and get nearest match from cache
            var indeces = GetIndeces(colors);

            return indeces;
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

        private void FillDistinctColors(IEnumerable<Color> colors)
        {
            _distinctColors = new ConcurrentDictionary<int, DistinctColorInfo>();

            foreach (var c in colors)
                _distinctColors.AddOrUpdate(c.ToArgb(), key => new DistinctColorInfo(c),
                    (key, info) => info.IncreaseCount());
        }

        private void CreateAndCachePalette()
        {
            // Step 1: Filter colors by hue, saturation and brightness
            // Step 1.1: If color count not reached, take top(n) colors
            var palette = FilterColorInfos();

            // Step 2: Cache filtered colors
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

        private IEnumerable<int> GetIndeces(IEnumerable<Color> colors)
        {
            var colorList = colors.ToArray();
            var indices = new int[colorList.Length];

            var elementCount = colorList.Length / _taskCount;
            var overflow = colorList.Length - elementCount * _taskCount;

            var tasks = new TaskModel[_taskCount];
            for (int i = 0; i < _taskCount; i++)
                tasks[i] = new TaskModel(colorList, indices, i * elementCount, elementCount + (i == _taskCount - 1 ? overflow : 0));

            Parallel.ForEach(tasks, RunTask);

            return indices;
        }

        private void RunTask(TaskModel taskModel)
        {
            for (int i = taskModel.Start; i < taskModel.Start + taskModel.Length; i++)
                taskModel.Indices[i] = _colorCache.GetPaletteIndex(taskModel.Colors[i]);
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
