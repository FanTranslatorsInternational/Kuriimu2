using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Kanvas.Quantization.Models.Ditherer;
using Kontract.Kanvas.Quantization;

namespace Kanvas.Quantization.Ditherers.ErrorDiffusion
{
    public abstract class BaseErrorDiffusionDitherer : IColorDitherer
    {
        private Size _imageSize;
        private readonly int _taskCount;
        private IColorCache _colorCache;

        private float[,] _errorFactorMatrix;

        protected abstract byte[,] Matrix { get; }
        protected abstract int MatrixSideWidth { get; }
        protected abstract int MatrixSideHeight { get; }
        protected abstract int ErrorLimit { get; }

        public BaseErrorDiffusionDitherer(Size imageSize, int taskCount)
        {
            _imageSize = imageSize;
            _taskCount = taskCount;

            PrepareErrorFactorMatrix();
        }

        private void PrepareErrorFactorMatrix()
        {
            var matrixWidth = Matrix.GetLength(1);
            var matrixHeight = Matrix.GetLength(0);

            _errorFactorMatrix = new float[matrixHeight, matrixWidth];
            for (var i = 0; i < matrixHeight; i++)
                for (var j = 0; j < matrixWidth; j++)
                    _errorFactorMatrix[i, j] = Matrix[i, j] / (float)ErrorLimit;
        }

        public IEnumerable<int> Process(IEnumerable<Color> colors, IColorCache colorCache)
        {
            _colorCache = colorCache;

            if (!(colors is IList<Color> colorList))
                colorList = colors.ToList();

            var errorComponents = new ColorComponentError[colorList.Count];
            var indices = new int[colorList.Count];
            var errors = new ErrorDiffusionList(colorList, errorComponents, indices);

            var delayedTasks = CreateDelayedTasks(errors).ToArray();
            ParallelProcessing.ProcessParallel(delayedTasks, _taskCount, delayedLineTask => delayedLineTask.Process(ProcessingAction));

            return indices;
        }

        private IEnumerable<ErrorDiffusionLineTask> CreateDelayedTasks(IList<ErrorDiffusionElement> errors)
        {
            ErrorDiffusionLineTask parent = null;
            for (var i = 0; i < _imageSize.Height; i++)
            {
                var delayedTask = new ErrorDiffusionLineTask(
                    errors, i * _imageSize.Width, _imageSize.Width, MatrixSideWidth + 1, parent);
                parent = delayedTask;

                yield return delayedTask;
            }
        }

        private void ProcessingAction(ErrorDiffusionLineTask task, int index)
        {
            // Get reference elements to work with
            var inputElement = task.Elements[index];
            var sourceColor = inputElement.Input;
            var error = inputElement.Error ?? new ColorComponentError();

            // Add Error component values to source color
            var errorDiffusedColor = Color.FromArgb(
                sourceColor.A,
                Clamp(sourceColor.R + error.RedError, 0, 255),
                Clamp(sourceColor.G + error.GreenError, 0, 255),
                Clamp(sourceColor.B + error.BlueError, 0, 255));

            // Quantize Error diffused source color
            task.Elements[index].PaletteIndex = _colorCache.GetPaletteIndex(errorDiffusedColor);

            // Retrieve new quantized color for this point
            var targetColor = _colorCache.Palette[task.Elements[index].PaletteIndex];

            // Calculate errors to distribute for this point
            var redError = errorDiffusedColor.R - targetColor.R;
            var greenError = errorDiffusedColor.G - targetColor.G;
            var blueError = errorDiffusedColor.B - targetColor.B;

            // Retrieve point position
            var pixelX = index % _imageSize.Width;
            var pixelY = index / _imageSize.Width;

            // Process the matrix
            for (var shiftY = -MatrixSideHeight; shiftY <= MatrixSideHeight; shiftY++)
            {
                for (var shiftX = -MatrixSideWidth; shiftX <= MatrixSideWidth; shiftX++)
                {
                    var targetX = pixelX + shiftX;
                    var targetY = pixelY + shiftY;
                    var coefficient = Matrix[shiftY + MatrixSideHeight, shiftX + MatrixSideWidth];
                    var errorFactor = _errorFactorMatrix[shiftY + MatrixSideHeight, shiftX + MatrixSideWidth];

                    // If substantial Error factor and target point in image bounds
                    if (coefficient != 0 &&
                        targetX >= 0 && targetX < _imageSize.Width &&
                        targetY >= 0 && targetY < _imageSize.Height)
                    {
                        // Add Error to target point for later processing
                        var newTarget = task.Elements[targetX + targetY * _imageSize.Width];
                        if (newTarget.Error == null)
                            newTarget.Error = new ColorComponentError();

                        newTarget.Error.RedError += Convert.ToInt32(errorFactor * redError);
                        newTarget.Error.GreenError += Convert.ToInt32(errorFactor * greenError);
                        newTarget.Error.BlueError += Convert.ToInt32(errorFactor * blueError);
                    }
                }

                inputElement.Error = null;
            }
        }

        // TODO: Remove when targeting only netcoreapp31
        private static int Clamp(int value, int min, int max)
        {
#if NET_CORE_31
            return Math.Clamp(value, min, max);
#else
            return Math.Max(min, Math.Min(value, max));
#endif
        }
    }
}
