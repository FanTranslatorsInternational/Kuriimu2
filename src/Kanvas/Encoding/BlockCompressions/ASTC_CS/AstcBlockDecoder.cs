using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kanvas.Encoding.BlockCompressions.ASTC_CS.Models;
using Kanvas.Support;
using Komponent.IO;
using Kontract.Models.IO;

namespace Kanvas.Encoding.BlockCompressions.ASTC_CS
{
    class AstcBlockDecoder
    {
        private IEnumerable<Color> ErrorColors => 
            Enumerable.Repeat(Constants.ErrorValue, _x * _y * _z);

        private readonly int _x;
        private readonly int _y;
        private readonly int _z;

        public AstcBlockDecoder(int x, int y, int z)
        {
            _x = x;
            _y = y;
            _z = z;
        }

        public IEnumerable<Color> DecodeBlocks(byte[] block)
        {
            // Find block containing texel
            var br = CreateReader(block);

            // Read block mode
            var blockMode = BlockMode.Create(br);

            // If void-extent, return single color (optimization)
            if (blockMode.IsVoidExtent)
            {
                var voidExtentColor = CreateVoidExtentColor(br, blockMode.IsHdr);
                return Enumerable.Repeat(voidExtentColor, _x * _y * _z);
            }

            // If a reserved block is used, return error color
            if (blockMode.UsesReserved)
                return ErrorColors;

            // If invalid weight ranges
            var weightBits = IntegerSequenceEncoding.ComputeBitCount(blockMode.WeightCount, blockMode.QuantizationMode);
            if (blockMode.WeightCount > Constants.MaxWeightsPerBlock ||
                weightBits < Constants.MinWeightBitsPerBlock ||
                weightBits > Constants.MaxWeightBitsPerBlock)
                return ErrorColors;

            /* For each plane in image
                  If block mode requires infill
                    Find and decode stored weights adjacent to texel, unquantize and
                        interpolate
                  Else
                    Find and decode weight for texel, and unquantize

                Read number of partitions
                If number of partitions > 1
                  Read partition table pattern index
                  Look up partition number from pattern

                Read color endpoint mode and endpoint data for selected partition
                Unquantize color endpoints
                Interpolate color endpoints using weight (or weights in dual-plane mode)
                Return interpolated color
             */
        }

        private BitReader CreateReader(byte[] block)
        {
            block = block.Reverse().ToArray();
            return new BitReader(new MemoryStream(block), BitOrder.LeastSignificantBitFirst, 1, ByteOrder.LittleEndian);
        }

        private Color CreateVoidExtentColor(BitReader br, bool isHdr)
        {
            // Detect illegal void-extent
            br.Position--;
            if (br.ReadBits<byte>(2) != 3)
                return Constants.ErrorValue;

            // Detect illegal texture coordinates
            var minS = br.ReadBits<short>(13);
            var maxS = br.ReadBits<short>(13);
            var minT = br.ReadBits<short>(13);
            var maxT = br.ReadBits<short>(13);
            if (minS != 0x1FFF || maxS != 0x1FFF || minT != 0x1FFF || maxT != 0x1FFF)
            {
                if (minS >= maxS || minT >= maxT)
                {
                    return Constants.ErrorValue;
                }
            }

            // Read constant color block
            var r = br.ReadBits<int>(16);
            var g = br.ReadBits<int>(16);
            var b = br.ReadBits<int>(16);
            var a = br.ReadBits<int>(16);
            if (isHdr)
            {
                // In IsHdr mode, components are stored as FP16, or half-precision floating point
                return Color.FromArgb((int)(Half)a, (int)(Half)r, (int)(Half)g, (int)(Half)b);
            }

            // Otherwise they are stored as UNORM16, or short and therefore needs to be sampled down to a range of 0..255
            // UNORM16 is used due to the possibility of sRGB color space, which isn't supported currently
            return Color.FromArgb(
                Conversion.ChangeBitDepth(a, 16, 8),
                Conversion.ChangeBitDepth(r, 16, 8),
                Conversion.ChangeBitDepth(g, 16, 8),
                Conversion.ChangeBitDepth(b, 16, 8));
        }
    }
}
