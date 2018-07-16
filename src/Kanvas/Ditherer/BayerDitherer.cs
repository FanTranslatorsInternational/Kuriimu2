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
        int[,] CachedMatrix = new int[,]
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

            CachedMatrix = new int[matrixSize, matrixSize];
        }

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
        //        {  0,  8,  2, 10 },
        //        { 12,  4, 14,  6 },
        //        {  3, 11,  1,  9 },
        //        { 15,  7, 13,  5 }
        //    };

        int[,] CreateMatrix(int matrixSize)
        {
            /*var res = new int[matrixSize, matrixSize];
            if (matrixSize == 1)
            {
                res[0, 0] = 0;
                return res;
            }

            var microSize = matrixSize / 2;
            var microArr=new int[] { }
            for (int i = 0; i < matrixSize * matrixSize; i++)
            {
                var microIndex = i % 4;

                var x=

                res[]
            }*/
            return null;
        }

        public IEnumerable<Color> Process(IEnumerable<Color> source, List<Color> palette) =>
            Support.OrderedDitherer.TransformColors(source, palette, MatrixWidth, MatrixHeight, Width, CachedMatrix);
    }
}
