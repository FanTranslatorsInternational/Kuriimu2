using System.Drawing;
using System.Linq;
using Kanvas.Encoding.BlockCompressions.ASTC_CS.Models;
using Kanvas.Encoding.BlockCompressions.ASTC_CS.Types;
using Kanvas.Support;

namespace Kanvas.Encoding.BlockCompressions.ASTC_CS.Colors
{
    static class ColorHelper
    {
        private static int[] _colorBits =
        {
            -1,
            115 - 4,
            113 - 4 - Constants.PartitionBits,
            113 - 4 - Constants.PartitionBits,
            113 - 4 - Constants.PartitionBits
        };

        public static int[][] QuantizationModeTable = BuildQuantizationModeTable();

        public static int CalculateColorBits(int partitionCount, int weigthBitCount, bool isDualPlane, int encodedTypeHighPartSize)
        {
            var colorBits = _colorBits[partitionCount] - weigthBitCount - encodedTypeHighPartSize;
            if (isDualPlane)
                colorBits -= 2;
            if (colorBits < 0)
                colorBits = 0;

            return colorBits;
        }

        private static int[][] BuildQuantizationModeTable()
        {
            var quantModeTable = new int[17][];

            for (var i = 0; i < 17; i++)
                quantModeTable[i] = Enumerable.Repeat(-1, 128).ToArray();

            for (var i = 0; i < 21; i++)
            {
                for (var j = 1; j <= 16; j++)
                {
                    var p = IntegerSequenceEncoding.ComputeBitCount(2 * j, i);
                    if (p < 128)
                        quantModeTable[j][p] = i;
                }
            }

            for (var i = 0; i <= 16; i++)
            {
                int largest_value_so_far = -1;
                for (var j = 0; j < 128; j++)
                {
                    if (quantModeTable[i][j] > largest_value_so_far)
                        largest_value_so_far = quantModeTable[i][j];
                    else
                        quantModeTable[i][j] = largest_value_so_far;
                }
            }

            return quantModeTable;
        }
    }
}
