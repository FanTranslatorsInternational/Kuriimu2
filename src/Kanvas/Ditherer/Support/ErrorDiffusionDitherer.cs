using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace Kanvas.Ditherer.Support
{
    public static class ErrorDiffusionDitherer
    {
        public static IEnumerable<Color> TransformColors(IEnumerable<Color> source, List<Color> palette, int MatrixSideWidth, int MatrixSideHeight, int ImageWidth, int ImageHeight, int[,] CachedMatrix)
        {
            var target = new List<Color>(source);

            var CachedSummedMatrix = CreateCachedSummedMatrix(CachedMatrix);

            var index = 0;
            foreach (var p in source)
            {
                var sourceCoord = new Point(index / ImageWidth, index % ImageWidth);

                var sourceColor = p;
                var targetColor = palette[OrderedDitherer.NearestColor(palette, p)];

                int redError = sourceColor.R - targetColor.R;
                int greenError = sourceColor.G - targetColor.G;
                int blueError = sourceColor.B - targetColor.B;

                // only propagate non-zero error
                if (redError != 0 || greenError != 0 || blueError != 0)
                {
                    // processes the matrix
                    for (int shiftY = -MatrixSideHeight; shiftY <= MatrixSideHeight; shiftY++)
                        for (int shiftX = -MatrixSideWidth; shiftX <= MatrixSideWidth; shiftX++)
                        {
                            int targetX = sourceCoord.X + shiftX;
                            int targetY = sourceCoord.Y + shiftY;
                            int coeficient = CachedMatrix[shiftY + MatrixSideHeight, shiftX + MatrixSideWidth];
                            float coeficientSummed = CachedSummedMatrix[shiftY + MatrixSideHeight, shiftX + MatrixSideWidth];

                            if (coeficient != 0 &&
                                targetX >= 0 && targetX < ImageWidth &&
                                targetY >= 0 && targetY < ImageHeight)
                            {
                                ProcessNeighbor(target, targetX + targetY * ImageWidth, coeficientSummed, redError, greenError, blueError);
                            }
                        }
                }

                index++;
            }

            return target;
        }

        private static void ProcessNeighbor(List<Color> target, int index, float factor, int redError, int greenError, int blueError)
        {
            Color oldColor = target[index];

            Int32 red = GetClampedColorElementWithError(oldColor.R, factor, redError);
            Int32 green = GetClampedColorElementWithError(oldColor.G, factor, greenError);
            Int32 blue = GetClampedColorElementWithError(oldColor.B, factor, blueError);
            Color newColor = Color.FromArgb(255, red, green, blue);

            target[index] = newColor;
        }

        private static int GetClampedColorElementWithError(int colorElement, float factor, int error)
        {
            int result = Convert.ToInt32(colorElement + factor * error);
            return OrderedDitherer.GetClampedColorElement(result);
        }

        private static float[,] CreateCachedSummedMatrix(int[,] CachedMatrix)
        {
            float maximum = GetMatrixFactor(CachedMatrix);

            int width = CachedMatrix.GetLength(1);
            int height = CachedMatrix.GetLength(0);
            var CachedSummedMatrix = new float[height, width];

            // caches the matrix (and division by a sum)
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    CachedSummedMatrix[y, x] = CachedMatrix[y, x] / maximum;
                }

            return CachedSummedMatrix;
        }

        private static int GetMatrixFactor(int[,] CachedMatrix)
        {
            int result = 0;

            for (int y = 0; y < CachedMatrix.GetLength(0); y++)
                for (int x = 0; x < CachedMatrix.GetLength(1); x++)
                {
                    int value = CachedMatrix[y, x];
                    if (value > result) result = value;
                }

            return result;
        }
    }
}
