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

        public string DithererName { get; set; }

        int[,] CachedMatrix = new int[,]
            {
                { 13,  5, 12, 16 },
                {  6,  0,  4, 11 },
                {  7,  2,  3, 10 },
                { 14,  8,  9, 15 }
            };
        byte MatrixWidth { get; set; }
        byte MatrixHeight { get; set; }

        public ClusteredDotDitherer()
        {
            MatrixWidth = MatrixHeight = 4;
        }

        public IEnumerable<Color> Process(IEnumerable<Color> source, List<Color> palette) =>
            Support.OrderedDitherer.TransformColors(source, palette, MatrixWidth, MatrixHeight, Width, CachedMatrix);
    }
}
