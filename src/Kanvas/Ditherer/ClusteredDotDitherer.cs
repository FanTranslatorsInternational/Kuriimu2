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
    /// 
    /// </summary>
    public class ClusteredDotDitherer : IDitherer
    {
        public int Width { get; set; }
        public int Height { get; set; }

        public string DithererName { get; private set; }

        int[,] CachedMatrix = new int[,]
            {
                { 13,  5, 12, 16 },
                {  6,  1,  4, 11 },
                {  7,  2,  3, 10 },
                { 14,  8,  9, 15 }
            };
        int[,] CachedMatrix2 = new int[,]
            {
                { 25,  9, 23, 31, 35, 45, 43, 33 },
                { 11,  1,  7, 21, 47, 59, 57, 41 },
                { 13,  3,  5, 19, 49, 61, 63, 55 },
                { 27, 15, 17, 29, 37, 51, 53, 39 },
                { 36, 46, 44, 34, 26, 10, 24, 32 },
                { 48, 60, 58, 42, 12,  2,  8, 22 },
                { 50, 62, 64, 56, 14,  4,  6, 20 },
                { 38, 52, 54, 40, 28, 16, 18, 30 }
            };
        byte MatrixWidth { get; set; }
        byte MatrixHeight { get; set; }

        public ClusteredDotDitherer()
        {
            MatrixWidth = MatrixHeight = 4;
            DithererName = "ClusteredDot4x4";
        }

        public IEnumerable<Color> Process(IEnumerable<Color> source, List<Color> palette) =>
            Support.OrderedDitherer.TransformColors(source, palette, MatrixWidth, MatrixHeight, Width, CachedMatrix);
    }
}
