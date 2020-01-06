using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Kanvas.Quantization.ColorCaches;
using Kanvas.Quantization.Helper;
using Kanvas.Quantization.Models.Parallel;
using Kanvas.Quantization.Models.Quantizer.Wu;
using Kontract.Kanvas.Quantization;

namespace Kanvas.Quantization.Quantizers
{
    public class WuColorQuantizer : IColorQuantizer
    {
        private readonly WuColorCache _colorCache;
        private readonly int _colorCount;

        private readonly Wu3DHistogram _histogram;

        private int _tableLength;

        /// <inheritdoc />
        public IColorCache ColorCache => _colorCache;

        /// <inheritdoc />
        public bool UsesVariableColorCount => true;

        /// <inheritdoc />
        public bool SupportsAlpha => true;

        /// <inheritdoc />
        public bool AllowParallel => true;

        /// <inheritdoc />
        public int TaskCount { get; set; }

        public WuColorQuantizer(int indexBits, int indexAlphaBits)
        {
            _colorCache = new WuColorCache(indexBits, indexAlphaBits);
            _histogram = new Wu3DHistogram(indexBits, indexAlphaBits, (1 << indexBits) + 1, (1 << indexAlphaBits) + 1);

            _tableLength = _histogram.IndexCount * _histogram.IndexCount * _histogram.IndexCount * _histogram.IndexAlphaCount;
            _colorCache.Tag = new byte[_tableLength];

            _colorCount = 256;
        }

        public IEnumerable<int> Process(IEnumerable<Color> colors)
        {
            var colorArray = colors.ToArray();

            // Step 1: Create palette
            CreatePalette(colorArray);

            // Step 2: Loop through original colors and deterine their indeces in the palette
            return GetIndeces(colorArray);
        }

        public IList<Color> CreatePalette(IEnumerable<Color> colors)
        {
            Array.Clear(_colorCache.Tag, 0, _tableLength);

            // Step 1: Build a 3-dimensional histogram of all colors and calculate their moments
            _histogram.Create(colors.ToList());

            // Step 2: Create color cube
            var cube = WuColorCube.Create(_histogram, _colorCount);

            // Step 3: Create palette from color cube
            var palette = CreatePalette(cube).ToList();

            _colorCache.CachePalette(palette);

            return palette;
        }

        private IEnumerable<Color> CreatePalette(WuColorCube cube)
        {
            for (int k = 0; k < cube.ColorCount; k++)
            {
                var box = cube.Boxes[k];
                Mark(box, (byte)k);

                var weight = box.GetPartialVolume(5);
                yield return weight == 0 ?
                    Color.Black :
                    Color.FromArgb(
                        (byte)(box.GetPartialVolume(4) / weight),
                        (byte)(box.GetPartialVolume(1) / weight),
                        (byte)(box.GetPartialVolume(2) / weight),
                        (byte)(box.GetPartialVolume(3) / weight));
            }
        }

        private void Mark(WuColorBox box, byte label)
        {
            for (int r = box.R0 + 1; r <= box.R1; r++)
            {
                for (int g = box.G0 + 1; g <= box.G1; g++)
                {
                    for (int b = box.B0 + 1; b <= box.B1; b++)
                    {
                        for (int a = box.A0 + 1; a <= box.A1; a++)
                        {
                            _colorCache.Tag[WuCommon.GetIndex(r, g, b, a, _histogram.IndexBits, _histogram.IndexAlphaBits)] = label;
                        }
                    }
                }
            }
        }

        // TODO: Make parallel better
        private IEnumerable<int> GetIndeces(IEnumerable<Color> colors)
        {
            var colorList = colors.ToArray();
            var indices = new int[colorList.Length];

            void ProcessingAction(LineTask<IList<Color>, int[]> taskModel)
            {
                for (int i = taskModel.Start; i < taskModel.Start + taskModel.Length; i++)
                    taskModel.Output[i] = _colorCache.GetPaletteIndex(taskModel.Input[i]);
            }

            ParallelProcessing.ProcessList(colorList, indices, ProcessingAction, TaskCount);

            return indices;
        }
    }
}
