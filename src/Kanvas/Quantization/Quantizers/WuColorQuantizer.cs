using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Kanvas.Quantization.Helper;
using Kanvas.Quantization.Interfaces;
using Kanvas.Quantization.Models.Parallel;
using Kanvas.Quantization.Models.Quantizer;
using Kanvas.Quantization.Models.Quantizer.Wu;

namespace Kanvas.Quantization.Quantizers
{
    public class WuColorQuantizer : IColorQuantizer
    {
        private int _colorCount;

        private int _indexBits;
        private int _indexAlphaBits;
        private int _indexCount;
        private int _indexAlphaCount;
        private int _tableLength;

        private IList<Color> _palette;

        /// <summary>
        /// Color space tag.
        /// </summary>
        private readonly byte[] tag;

        #region IColorQuantizer

        /// <inheritdoc cref="IColorQuantizer.UsesColorCache"/>
        public bool UsesColorCache => false;

        /// <inheritdoc cref="IColorQuantizer.UsesVariableColorCount"/>
        public bool UsesVariableColorCount => true;

        /// <inheritdoc cref="IColorQuantizer.SupportsAlpha"/>
        public bool SupportsAlpha => true;

        /// <inheritdoc cref="IColorQuantizer.AllowParallel"/>
        public bool AllowParallel => true;

        public int TaskCount { get; private set; } = 8;

        /// <inheritdoc cref="IColorQuantizer.SetColorCache(IColorCache)"/>
        public void SetColorCache(IColorCache colorCache)
        {
            throw new NotSupportedException();
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

            // Step 0: Clear instance
            Clear();

            // Step 1: Create palette
            CreatePalette(colorArray);

            // Step 2: Loop through original colors and deterine their indeces in the palette
            return GetIndeces(colorArray);
        }

        public int GetPaletteIndex(Color color)
        {
            int a = color.A >> (8 - _indexAlphaBits);
            int r = color.R >> (8 - _indexBits);
            int g = color.G >> (8 - _indexBits);
            int b = color.B >> (8 - _indexBits);

            int index = WuCommon.GetIndex(r + 1, g + 1, b + 1, a + 1, _indexBits, _indexAlphaBits);

            return tag[index];
        }

        public void CreatePalette(IEnumerable<Color> colors)
        {
            // Step 1: Build a 3-dimensional histogram of all colors and calculate their moments
            var histogram = new Wu3DHistogram(colors.ToArray(), _indexBits, _indexAlphaBits, _indexCount, _indexAlphaCount);

            // Step 2: Create color cube
            var cube = WuColorCube.Create(histogram, _colorCount);

            // Step 3: Create palette from color cube
            _palette = CreatePalette(cube).ToList();
        }

        public IList<Color> GetPalette()
        {
            return _palette;
        }

        #endregion

        public WuColorQuantizer(int indexBits, int indexAlphaBits)
        {
            _indexBits = indexBits;
            _indexAlphaBits = indexAlphaBits;
            _indexCount = (1 << _indexBits) + 1;
            _indexAlphaCount = (1 << _indexAlphaBits) + 1;

            _tableLength = _indexCount * _indexCount * _indexCount * _indexAlphaCount;

            tag = new byte[_tableLength];
        }

        /// <summary>
        /// Clears the tables.
        /// </summary>
        private void Clear()
        {
            _palette = null;

            Array.Clear(tag, 0, _tableLength);
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
                            tag[WuCommon.GetIndex(r, g, b, a, _indexBits, _indexAlphaBits)] = label;
                        }
                    }
                }
            }
        }

        private IEnumerable<int> GetIndeces(IEnumerable<Color> colors)
        {
            var colorList = colors.ToArray();
            var indices = new int[colorList.Length];

            void ProcessingAction(LineTask<Color[], int[]> taskModel)
            {
                for (int i = taskModel.Start; i < taskModel.Start + taskModel.Length; i++)
                    taskModel.Output[i] = GetPaletteIndex(taskModel.Input[i]);
            }

            ParallelProcessing.ProcessList(colorList, indices, ProcessingAction, TaskCount);

            return indices;
        }
    }
}
