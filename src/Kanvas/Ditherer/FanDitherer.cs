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
    public class FanDitherer : IDitherer
    {
        public int Width { get; set; }
        public int Height { get; set; }

        public string DithererName { get; private set; }

        int[,] CachedMatrix = new int[,] {
            { 0, 0, 0, 0, 0, 0, 0 },
            { 0, 0, 0, 0, 8, 0, 0 },
            { 1, 1, 2, 4, 0, 0, 0 }
        };
        byte MatrixSideWidth { get; set; }
        byte MatrixSideHeight { get; set; }

        public FanDitherer()
        {
            MatrixSideWidth = 3;
            MatrixSideHeight = 1;
            DithererName = $"FanDitherer{MatrixSideWidth * 2 + 1}x{MatrixSideHeight * 2 + 1}";
        }

        public IEnumerable<Color> Process(IEnumerable<Color> source, List<Color> palette) =>
            Support.ErrorDiffusionDitherer.TransformColors(source, palette, MatrixSideWidth, MatrixSideHeight, Width, Height, CachedMatrix);
    }
}
