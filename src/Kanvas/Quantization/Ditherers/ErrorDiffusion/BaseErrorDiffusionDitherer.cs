using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Kanvas.Quantization.Helper;
using Kanvas.Quantization.Interfaces;
using Kanvas.Quantization.Models;
using Kanvas.Quantization.Models.Ditherer;
using Kanvas.Quantization.Models.Parallel;

namespace Kanvas.Quantization.Ditherers.ErrorDiffusion
{
    public abstract class BaseErrorDiffusionDitherer : IColorDitherer
    {
        private IColorQuantizer _quantizer;
        private int _width;
        private int _height;

        protected abstract byte[,] Matrix { get; }
        protected abstract int MatrixSideWidth { get; }
        protected abstract int MatrixSideHeight { get; }
        protected abstract int ErrorLimit { get; }

        protected float[,] ErrorFactorMatrix { get; private set; }

        public void Prepare(IColorQuantizer quantizer, int width, int height)
        {
            _quantizer = quantizer;
            _width = width;
            _height = height;

            PrepareErrorFactorMatrix();
        }

        private void PrepareErrorFactorMatrix()
        {
            var matrixWidth = Matrix.GetLength(1);
            var matrixHeight = Matrix.GetLength(0);

            ErrorFactorMatrix = new float[matrixHeight, matrixWidth];
            for (int i = 0; i < matrixHeight; i++)
                for (int j = 0; j < matrixWidth; j++)
                    ErrorFactorMatrix[i, j] = Matrix[i, j] / (float)ErrorLimit;
        }

        public IEnumerable<int> Process(IEnumerable<Color> colors)
        {
            if (_quantizer == null)
                throw new ArgumentNullException(nameof(_quantizer));
            if (ErrorFactorMatrix == null)
                throw new ArgumentNullException(nameof(ErrorFactorMatrix));

            var colorList = colors.ToArray();
            _quantizer.CreatePalette(colorList);

            var indices = new int[colorList.Length];
            var errorComponents = new ColorComponentError[colorList.Length];
            for (int i = 0; i < errorComponents.Length; i++)
                errorComponents[i] = new ColorComponentError(0, 0, 0);
            var errors =
                new ErrorDiffusionList<Color, ColorComponentError>(colorList, errorComponents)
                    .ToArray();

            ParallelProcessing.ProcessList(
                errors, indices, _width,
                MatrixSideWidth + 1, _quantizer.TaskCount, ProcessingAction);

            return indices;
        }

        private void ProcessingAction(DelayedLineTask<ErrorDiffusionElement<Color, ColorComponentError>, int[]> delayedLineTask, int index)
        {
            // Get reference elements to work with
            var inputElement = delayedLineTask.Input[index];
            var sourceColor = inputElement.Input;
            var error = inputElement.Error;

            // Add Error component values to source color
            var errorDiffusedColor = Color.FromArgb(
                sourceColor.A,
                GetClampedValue(sourceColor.R + error.RedError, 0, 255),
                GetClampedValue(sourceColor.G + error.GreenError, 0, 255),
                GetClampedValue(sourceColor.B + error.BlueError, 0, 255));

            // Quantize Error diffused source color
            delayedLineTask.Output[index] = _quantizer.GetPaletteIndex(errorDiffusedColor);

            // Retrieve new quantized color for this point
            var targetColor = _quantizer.GetPalette()[delayedLineTask.Output[index]];

            // Calculate errors to distribute for this point
            int redError = errorDiffusedColor.R - targetColor.R;
            int greenError = errorDiffusedColor.G - targetColor.G;
            int blueError = errorDiffusedColor.B - targetColor.B;

            // Retrieve point position
            var pixelX = index % _width;
            var pixelY = index / _width;

            // Process the matrix
            for (int shiftY = -MatrixSideHeight; shiftY <= MatrixSideHeight; shiftY++)
                for (int shiftX = -MatrixSideWidth; shiftX <= MatrixSideWidth; shiftX++)
                {
                    int targetX = pixelX + shiftX;
                    int targetY = pixelY + shiftY;
                    var coefficient = Matrix[shiftY + MatrixSideHeight, shiftX + MatrixSideWidth];
                    var errorFactor = ErrorFactorMatrix[shiftY + MatrixSideHeight, shiftX + MatrixSideWidth];

                    // If substantial Error factor and target point in image bounds
                    if (coefficient != 0 &&
                        targetX >= 0 && targetX < _width &&
                        targetY >= 0 && targetY < _height)
                    {
                        // Add Error to target point for later processing
                        var newTarget = delayedLineTask.Input[targetX + targetY * _width];
                        newTarget.Error.RedError += Convert.ToInt32(errorFactor * redError);
                        newTarget.Error.GreenError += Convert.ToInt32(errorFactor * greenError);
                        newTarget.Error.BlueError += Convert.ToInt32(errorFactor * blueError);
                    }
                }
        }

        //protected abstract byte[,] Matrix { get; }
        //protected float[,] SummedMatrix { get; private set; }

        //protected abstract int MatrixSideWidth { get; }
        //protected abstract int MatrixSideHeight { get; }

        //public void Prepare(IColorQuantizer quantizer, int width, int height)
        //{
        //    _quantizer = quantizer;
        //    _width = width;
        //    _height = height;

        //    PrepareSummedMatrix();
        //}

        //private void PrepareSummedMatrix()
        //{
        //    // creates coeficient matrix and determines the matrix factor/divisor/maximum
        //    float maximum = GetMatrixFactor();

        //    // prepares the cache arrays
        //    int width = Matrix.GetLength(1);
        //    int height = Matrix.GetLength(0);
        //    SummedMatrix = new float[height, width];

        //    // caches the matrix (and division by a sum)
        //    for (var y = 0; y < height; y++)
        //        for (var x = 0; x < width; x++)
        //        {
        //            SummedMatrix[y, x] = Matrix[y, x] / maximum;
        //        }
        //}

        //private int GetMatrixFactor()
        //{
        //    int result = 0;

        //    for (int y = 0; y < Matrix.GetLength(0); y++)
        //    for (int x = 0; x < Matrix.GetLength(1); x++)
        //    {
        //        int value = Matrix[y, x];
        //        if (value > result) result = value;
        //    }

        //    return result;
        //}

        //public IEnumerable<int> Process(IEnumerable<Color> colors)
        //{
        //    if (_quantizer == null)
        //        throw new ArgumentNullException(nameof(_quantizer));
        //    if (SummedMatrix == null)
        //        throw new ArgumentNullException(nameof(SummedMatrix));

        //    var colorList = colors.ToArray();

        //    // Quantize image
        //    var indices = _quantizer.Process(colorList).ToArray();

        //    var ProcessingAction = new Action<TaskModel<Color[], int[]>>(taskModel =>
        //    {
        //        for (int i = taskModel.Start; i < taskModel.Start + taskModel.Length; i++)
        //        {
        //            // Retrieve the palette
        //            var palette = _quantizer.GetPalette();

        //            // Retrieve source and target color
        //            var sourceColor = taskModel.Input[i];
        //            int targetIndex;
        //            lock (_lock) targetIndex = taskModel.Output[i];
        //            var targetColor = palette[targetIndex];

        //            // Retrieve pixel position
        //            var pixelX = i % _width;
        //            var pixelY = i / _width;

        //            // Calculate color component errors
        //            int redError = sourceColor.R - targetColor.R;
        //            int greenError = sourceColor.G - targetColor.G;
        //            int blueError = sourceColor.B - targetColor.B;

        //            // if no Error, continue
        //            if (redError == 0 && greenError == 0 && blueError == 0)
        //                continue;

        //            // processes the matrix
        //            for (int shiftY = -MatrixSideHeight; shiftY <= MatrixSideHeight; shiftY++)
        //                for (int shiftX = -MatrixSideWidth; shiftX <= MatrixSideWidth; shiftX++)
        //                {
        //                    int targetX = pixelX + shiftX;
        //                    int targetY = pixelY + shiftY;
        //                    byte coefficient = Matrix[shiftY + MatrixSideHeight, shiftX + MatrixSideWidth];
        //                    float coefficientSummed =
        //                        SummedMatrix[shiftY + MatrixSideHeight, shiftX + MatrixSideWidth];

        //                    if (coefficient != 0 &&
        //                        targetX >= 0 && targetX < _width &&
        //                        targetY >= 0 && targetY < _height)
        //                    {
        //                        ProcessNeighbor(indices, targetX + targetY * _width, targetColor, coefficientSummed, redError, greenError,
        //                            blueError);
        //                    }
        //                }
        //        }
        //    });

        //    ParallelProcessing.ProcessList(colorList, indices, ProcessingAction, _quantizer.TaskCount);

        //    return indices;
        //}

        //private object _lock = new Object();
        //private void ProcessNeighbor(int[] indices, int targetIndex, Color oldColor, float factor, int redError, int greenError, int blueError)
        //{
        //    int red = GetClampedColorElementWithError(oldColor.R, factor, redError);
        //    int green = GetClampedColorElementWithError(oldColor.G, factor, greenError);
        //    int blue = GetClampedColorElementWithError(oldColor.B, factor, blueError);

        //    Color newColor = Color.FromArgb(255, red, green, blue);
        //    var newIndex = _quantizer.GetPaletteIndex(newColor);
        //    lock (_lock) indices[targetIndex] = newIndex;
        //}

        private int GetClampedColorElementWithError(int colorElement, float factor, int error)
        {
            int result = Convert.ToInt32(colorElement + factor * error);
            return GetClampedValue(result, 0, 255);
        }

        private int GetClampedValue(int value, int min, int max)
        {
            return Math.Min(Math.Max(value, min), max);
        }
    }
}
