using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kanvas.Interface;
using System.Drawing;

namespace Kanvas.Ditherer
{
    /// <summary>
    /// Brightness Ditherer
    /// </summary>
    public class BayerDitherer : IDitherer
    {
        public int Width { get; set; }
        public int Height { get; set; }

        public string DithererName { get => "Bayer"; }

        //byte[,] CachedMatrix = new byte[,] {
        //        { 0, 32,  8, 40,  2, 34, 10, 42},      /* 8x8 Bayer ordered dithering */
        //       {48, 16, 56, 24, 50, 18, 58, 26},      /* pattern. Each input pixel */
        //        {12, 44,  4, 36, 14, 46,  6, 38},      /* is scaled to the 0..63 range */
        //       {60, 28, 52, 20, 62, 30, 54, 22},      /* before looking in this table */
        //        { 3, 35, 11, 43,  1, 33,  9, 41},      /* to determine the action. */
        //        {51, 19, 59, 27, 49, 17, 57, 25},
        //        {15, 47,  7, 39, 13, 45,  5, 37},
        //       {63, 31, 55, 23, 61, 29, 53, 21}
        //};

        //byte[,] CachedMatrix = new byte[,]
        //    {
        //        {  1,  9,  3, 11 },
        //        { 13,  5, 15,  7 },
        //        {  4, 12,  2, 10 },
        //        { 16,  8, 14,  6 }
        //    };
        byte[,] CachedMatrix = new byte[,]
        {
            {  0, 2 },
            { 3, 1 }
        };
        byte MatrixWidth { get; set; }
        byte MatrixHeight { get; set; }

        public BayerDitherer(int matrixSize)
        {
            if (!((matrixSize != 0) && ((matrixSize & (matrixSize - 1)) == 0)))
                throw new InvalidOperationException("Matrix needs to be a power of 2.");

            MatrixWidth = MatrixHeight = (byte)matrixSize;

            CachedMatrix = new byte[matrixSize, matrixSize];
        }

        public IEnumerable<Color> Process(IEnumerable<Color> source, List<Color> palette)
        {
            var target = new List<Color>(source);

            int index = 0;
            foreach (var p in source)
            {
                Point srcP = new Point(index % Width, index / Width);

                // retrieves matrix coordinates
                int x = srcP.X % MatrixWidth;
                int y = srcP.Y % MatrixHeight;

                // reads the source pixel
                Color oldColor = p;

                // converts alpha to solid color
                oldColor = Color.FromArgb(255, oldColor.R, oldColor.G, oldColor.B);

                // determines the threshold
                var threshold = CachedMatrix[x, y];

                // only process dithering if threshold is substantial
                if (threshold > 0)
                {
                    int red = GetClampedColorElement(oldColor.R + threshold);
                    int green = GetClampedColorElement(oldColor.G + threshold);
                    int blue = GetClampedColorElement(oldColor.B + threshold);

                    Color newColor = Color.FromArgb(255, red, green, blue);

                    target[index] = palette[NearestColor(palette, newColor)];
                }

                index++;
            }

            return target;
        }

        // closed match in RGB space
        int NearestColor(List<Color> colors, Color target)
        {
            var colorDiffs = colors.Select(n => ColorDiff(n, target)).Min(n => n);
            return colors.FindIndex(n => ColorDiff(n, target) == colorDiffs);
        }

        // distance in RGB space
        int ColorDiff(Color c1, Color c2)
        {
            return (int)Math.Sqrt((c1.R - c2.R) * (c1.R - c2.R)
                                   + (c1.G - c2.G) * (c1.G - c2.G)
                                   + (c1.B - c2.B) * (c1.B - c2.B));
        }

        float FindClosest(int x, int y, float c0)
        {
            var dither = new int[][]{
                new int[]{ 0, 32,  8, 40,  2, 34, 10, 42},      /* 8x8 Bayer ordered dithering */
                new int[]{48, 16, 56, 24, 50, 18, 58, 26},      /* pattern. Each input pixel */
                new int[]{12, 44,  4, 36, 14, 46,  6, 38},      /* is scaled to the 0..63 range */
                new int[]{60, 28, 52, 20, 62, 30, 54, 22},      /* before looking in this table */
                new int[]{ 3, 35, 11, 43,  1, 33,  9, 41},      /* to determine the action. */
                new int[]{51, 19, 59, 27, 49, 17, 57, 25},
                new int[]{15, 47,  7, 39, 13, 45,  5, 37},
                new int[]{63, 31, 55, 23, 61, 29, 53, 21}
            };

            float limit = 0f;
            if (x < 8)
                limit = (dither[x][y] + 1) / 64f;


            if (c0 < limit)
                return 0f;
            return 1f;
        }

        protected int GetClampedColorElement(int colorElement)
        {
            int result = colorElement;
            if (result < 0) result = 0;
            if (result > 255) result = 255;
            return result;
        }
    }
}
