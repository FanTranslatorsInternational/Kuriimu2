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

        public string DithererName { get; private set; }

        int[,] CachedMatrix;
        byte MatrixWidth { get; set; }
        byte MatrixHeight { get; set; }

        public BayerDitherer(int matrixSize)
        {
            if (!((matrixSize != 0) && ((matrixSize & (matrixSize - 1)) == 0)))
                throw new InvalidOperationException("Matrix needs to be a power of 2.");

            MatrixWidth = MatrixHeight = (byte)matrixSize;
            DithererName = $"Bayer{matrixSize}x{matrixSize}";

            CachedMatrix = CreateMatrix(matrixSize);
        }

        int[,] CreateMatrix(int matrixSize)
        {
            var res = new int[matrixSize, matrixSize];
            if (matrixSize == 1)
            {
                res[0, 0] = 0;
                return res;
            }

            for (int y = 0; y < matrixSize; y++)
                for (int x = 0; x < matrixSize; x++)
                    res[y, x] = GetEntry(x, y, (int)Math.Log(matrixSize, 2)) + 1;

            return res;
        }
        int GetEntry(int x, int y, int bits) => BitInterleave(BitReverse(x ^ y, bits), BitReverse(y, bits), bits);
        int BitReverse(int x, int bits)
        {
            var res = 0;
            for (int i = 0; i < bits; i++)
                res = (res << 1) | ((x >> i) & 1);
            return res;
        }
        int BitInterleave(int x, int y, int bits)
        {
            int res = 0;
            for (int i = bits - 1; i >= 0; i--)
                res = (res << 2) | (((x >> i) & 1) << 1) | ((y >> i) & 1);
            return res;
        }

        public IEnumerable<Color> Process(IEnumerable<Color> source, List<Color> palette) =>
            Support.OrderedDitherer.TransformColors(source, palette, MatrixWidth, MatrixHeight, Width, CachedMatrix);
    }
}
