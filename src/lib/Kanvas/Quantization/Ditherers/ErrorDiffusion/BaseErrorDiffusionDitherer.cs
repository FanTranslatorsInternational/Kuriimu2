﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using Kanvas.MoreEnumerable;
using Kanvas.Quantization.Models.Ditherer;
using Kontract.Kanvas.Interfaces.Quantization;

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

            var count = _imageSize.Width * _imageSize.Height;
            var errorComponents = new ConcurrentDictionary<int, ColorComponentError>();
            var indices = new int[count];

            var delayedTasks = CreateDelayedTasks(colors, errorComponents, indices);
            ParallelProcessing.ProcessParallel(delayedTasks, _taskCount, delayedLineTask => delayedLineTask.Process(ProcessingAction));

            return indices;
        }

        private IEnumerable<ErrorDiffusionLineTask> CreateDelayedTasks(IEnumerable<Color> colors, IDictionary<int, ColorComponentError> errors, IList<int> indices)
        {
            var startIndex = 0;

            ErrorDiffusionLineTask parent = null;
            foreach (var colorLine in colors.Batch(_imageSize.Width))
            {
                var colorLineList = colorLine.ToList();
                var errorElements = colorLineList.Select((c, index) =>
                     new ErrorDiffusionElement(colorLineList, index, errors, indices));

                var delayedTask = new ErrorDiffusionLineTask(
                    errorElements, startIndex, _imageSize.Width, MatrixSideWidth + 1, parent);

                parent = delayedTask;
                startIndex += _imageSize.Width;

                yield return delayedTask;
            }
        }

        private void ProcessingAction(ErrorDiffusionElement element, int index)
        {
            // Get reference elements to work with
            var sourceColor = element.Color;
            if (!element.Errors.ContainsKey(index))
                element.Errors.Add(index, new ColorComponentError());
            var error = element.Errors[index];

            // Add Error component Values to source color
            var errorDiffusedColor = Color.FromArgb(
                sourceColor.A,
                Math.Clamp(sourceColor.R + error.RedError, 0, 255),
                Math.Clamp(sourceColor.G + error.GreenError, 0, 255),
                Math.Clamp(sourceColor.B + error.BlueError, 0, 255));

            // Quantize Error diffused source color
            element.Indices[index] = _colorCache.GetPaletteIndex(errorDiffusedColor);

            // Retrieve new quantized color for this point
            var targetColor = _colorCache.Palette[element.Indices[index]];

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
                        var newIndex = targetX + targetY * _imageSize.Width;
                        if (!element.Errors.ContainsKey(newIndex))
                            element.Errors.Add(newIndex, new ColorComponentError());
                        var newTarget = element.Errors[newIndex];

                        newTarget.RedError += Convert.ToInt32(errorFactor * redError);
                        newTarget.GreenError += Convert.ToInt32(errorFactor * greenError);
                        newTarget.BlueError += Convert.ToInt32(errorFactor * blueError);
                    }
                }

                element.Errors.Remove(index);
            }
        }
    }
}
