using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kanvas.Quantization.Helper;
using Kanvas.Quantization.Models;
using Kanvas.Quantization.Models.Parallel;
using Kontract.Kanvas.Quantization;

namespace Kanvas.Quantization.Ditherers.Ordered
{
    public abstract class BaseOrderDitherer : IColorDitherer
    {
        private readonly int _width;

        protected abstract byte[,] Matrix { get; }

        public int TaskCount { get; set; }

        public BaseOrderDitherer(int width)
        {
            _width = width;
        }

        public IEnumerable<int> Process(IEnumerable<Color> colors, IColorCache colorCache)
        {
            var processingAction = new Action<LineTask<IList<Color>, int[]>>(taskModel =>
            {
                var matrixWidth = Matrix.GetLength(0);
                var matrixHeight = Matrix.GetLength(1);

                for (int i = taskModel.Start; i < taskModel.Start + taskModel.Length; i++)
                {
                    int x = i % _width % matrixWidth;
                    int y = i / _width % matrixHeight;

                    int threshold = Convert.ToInt32(Matrix[x, y]);
                    var color = taskModel.Input[i];

                    int red = GetClampedValue(color.R + threshold, 0, 255);
                    int green = GetClampedValue(color.G + threshold, 0, 255);
                    int blue = GetClampedValue(color.B + threshold, 0, 255);

                    taskModel.Output[i] = colorCache.GetPaletteIndex(Color.FromArgb(color.A, red, green, blue));
                }
            });

            var colorList = colors.ToList();
            var indices = new int[colorList.Count];
            ParallelProcessing.ProcessList(colorList, indices, processingAction, TaskCount);

            return indices;
        }

        private int GetClampedValue(int value, int min, int max)
        {
            return Math.Min(Math.Max(value, min), max);
        }
    }
}
