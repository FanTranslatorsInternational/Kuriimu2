using Kanvas.Contract;
using Kanvas.Contract.DataClasses;
using Kanvas.Contract.Encoding;
using Kanvas.Encoding;
using SixLabors.ImageSharp;

namespace Kanvas.Swizzle
{
    /// <summary>
    /// Defines the swizzle used on the Nintendo Switch.
    /// </summary>
    public class NxSwizzle : IImageSwizzle
    {
        // Observed patterns:
        // 1. Max size and y extension are multiplied by 4 to regular limitations
        // 2. Bit Fields contain more elements the lower the bit depth
        // 2.1 One more element per possible halving starting from bit depth 32
        // 3. Comparing bit fields reveals that diagonal values are either equal or a half of the higher value

        // Possible origins for patterns:
        // 1. Block compression have 4-multiplied limitations due to the nature of 4x4 blocks of BC
        //    Since block compressions require a 4x4 linear layout, they aren't actually part of the NxSwizzle and are therefore "skipped", resulting in the multiplication of 4

        private const int BlockMaxSize_ = 512;
        private const int RegularMaxSize_ = 128;

        private const int BlockYExtensionStart_ = 32;
        private const int RegularYExtensionStart_ = 8;

        private static readonly Dictionary<string, (int, int)> AstcBlock = new()
        {
            ["ASTC 4x4"] = (4, 4),
            ["ASTC 5x4"] = (5, 4),
            ["ASTC 5x5"] = (5, 5),
            ["ASTC 6x5"] = (6, 5),
            ["ASTC 6x6"] = (6, 6),
            ["ASTC 8x5"] = (8, 5),
            ["ASTC 8x6"] = (8, 6),
            ["ASTC 8x8"] = (8, 8),
            ["ASTC 10x5"] = (10, 5),
            ["ASTC 10x6"] = (10, 6),
            ["ASTC 10x8"] = (10, 8),
            ["ASTC 10x10"] = (10, 10),
            ["ASTC 12x10"] = (12, 10),
            ["ASTC 12x12"] = (12, 12)
        };

        // TODO: Coords for block based encodings are prepended by the preparation method
        private static readonly Dictionary<int, (int, int)[]> CoordsBlock = new()
        {
            [04] = [(1, 0), (2, 0), (0, 1), (0, 2), (4, 0), (0, 4), (8, 0), (0, 8), (0, 16), (16, 0)],
            [08] = [(1, 0), (2, 0), (0, 1), (0, 2), (0, 4), (4, 0), (0, 8), (0, 16), (8, 0)]
        };

        private static readonly Dictionary<int, (int, int)[]> CoordsRegular = new()
        {
            [08] = [(1, 0), (2, 0), (4, 0), (8, 0), (0, 1), (16, 0), (0, 2), (0, 4), (32, 0)],
            [16] = [(1, 0), (2, 0), (4, 0), (0, 1), (8, 0), (0, 2), (0, 4), (16, 0)],
            [32] = [(1, 0), (2, 0), (0, 1), (4, 0), (0, 2), (0, 4), (8, 0)],
        };

        private readonly MasterSwizzle _swizzle;

        /// <inheritdoc />
        public int Width { get; }

        /// <inheritdoc />
        public int Height { get; }

        /// <inheritdoc />
        public int MacroTileWidth => _swizzle.MacroTileWidth;

        /// <inheritdoc />
        public int MacroTileHeight => _swizzle.MacroTileHeight;

        public NxSwizzle(SwizzleOptions context, int maxBitExtensionCount = -1)
        {
            var isBlockCompression = context.EncodingInfo is Bc;
            var bitDepth = context.EncodingInfo.BitDepth;
            var baseBitField = isBlockCompression ? CoordsBlock[bitDepth] : CoordsRegular[bitDepth];

            // Expand baseBitField to a max macro block height
            var maxSize = isBlockCompression ? BlockMaxSize_ : RegularMaxSize_;
            var startY = isBlockCompression ? BlockYExtensionStart_ : RegularYExtensionStart_;

            var bitFieldExtension = new List<(int, int)>();
            if (maxBitExtensionCount < 0)
            {
                for (var i = startY; i < Math.Min(context.Size.Height, maxSize); i *= 2)
                    bitFieldExtension.Add((0, i));
            }
            else
            {
                for (var j = 0; j < maxBitExtensionCount; j++)
                {
                    if (startY >= Math.Min(context.Size.Height, maxSize))
                        break;

                    bitFieldExtension.Add((0, startY));
                    startY *= 2;
                }
            }

            _swizzle = new MasterSwizzle(context.Size.Width, Point.Empty, baseBitField.Concat(bitFieldExtension).ToArray());

            (Width, Height) = PadSize(context, _swizzle);
        }

        /// <inheritdoc />
        public Point Transform(Point point) => Get(point.Y * Width + point.X);

        /// <inheritdoc />
        public Point Get(int pointCount) => _swizzle.Get(pointCount);

        private (int, int) PadSize(SwizzleOptions options, MasterSwizzle swizzle)
        {
            var width = SizePadding.Multiple(options.Size.Width, swizzle.MacroTileWidth);
            var height = SizePadding.Multiple(options.Size.Height, swizzle.MacroTileHeight);

            // Default case
            if (!AstcBlock.TryGetValue(options.EncodingInfo.FormatName, out (int, int) astcBlock))
                return (width, height);

            // Pad for ASTC
            var restWidth = width % astcBlock.Item1;
            width = width + (restWidth != 0 ? astcBlock.Item1 - restWidth : 0);

            var restHeight = height % astcBlock.Item2;
            height = height + (restHeight != 0 ? astcBlock.Item2 - restHeight : 0);

            return (width, height);

        }
    }
}
