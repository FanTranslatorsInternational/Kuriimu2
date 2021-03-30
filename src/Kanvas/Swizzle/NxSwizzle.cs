using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using Kanvas.Encoding;
using Kontract.Kanvas;
using Kontract.Kanvas.Model;

namespace Kanvas.Swizzle
{
    /// <summary>
    /// Defines the swizzle used on the Nintendo Switch.
    /// </summary>
    public class NxSwizzle : IImageSwizzle
    {
        private const int BlockMaxSize_ = 512;
        private const int RegularMaxSize_ = 128;

        private static readonly Dictionary<string, (int, int)> AstcBlock = new Dictionary<string, (int, int)>
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
        private static readonly Dictionary<int, (int, int)[]> CoordsBlock = new Dictionary<int, (int, int)[]>
        {
            [4] = new[] { (1, 0), (2, 0), (0, 1), (0, 2), (4, 0), (0, 4), (8, 0), (0, 8), (0, 16), (16, 0) },
            [8] = new[] { (1, 0), (2, 0), (0, 1), (0, 2), (0, 4), (4, 0), (0, 8), (0, 16), (8, 0) }
        };

        private static readonly Dictionary<int, (int, int)[]> CoordsRegular = new Dictionary<int, (int, int)[]>
        {
            [32] = new[] { (1, 0), (2, 0), (0, 1), (4, 0), (0, 2), (0, 4), (8, 0), (0, 8), (0, 16) }
        };

        private readonly MasterSwizzle _swizzle;

        public int Width { get; }
        public int Height { get; }

        public NxSwizzle(SwizzlePreparationContext context)
        {
            (Width, Height) = PadSizeToBlocks(context.Size.Width, context.Size.Height, context.EncodingInfo);

            var isBlockCompression = context.EncodingInfo is Bc;
            var bitDepth = context.EncodingInfo.BitDepth;
            var baseBitField = isBlockCompression ? CoordsBlock[bitDepth] : CoordsRegular[bitDepth];

            // Expand baseBitField to a max macro block height
            var bitFieldExtension = new List<(int, int)>();
            var maxSize = isBlockCompression ? BlockMaxSize_ : RegularMaxSize_;
            for (var i = 32; i < Math.Min(Height, maxSize); i *= 2)
                bitFieldExtension.Add((0, i));

            _swizzle = new MasterSwizzle(context.Size.Width, Point.Empty, baseBitField.Concat(bitFieldExtension).ToArray());
        }

        /// <inheritdoc />
        public Point Transform(Point point) => _swizzle.Get(point.Y * Width + point.X);

        private (int, int) PadSizeToBlocks(int width, int height, IEncodingInfo encodingInfo)
        {
            // Pad for Block Compression
            var isBlockCompression = encodingInfo is Bc;
            if (isBlockCompression)
                return ((width + 3) & ~3, (height + 3) & ~3);

            // Default case
            if (!AstcBlock.ContainsKey(encodingInfo.FormatName)) 
                return (width, height);

            // Pad for ASTC
            var astcBlock = AstcBlock[encodingInfo.FormatName];

            var restWidth = width % astcBlock.Item1;
            var newWidth = width + (restWidth != 0 ? astcBlock.Item1 - restWidth : 0);

            var restHeight = height % astcBlock.Item2;
            var newHeight = height + (restHeight != 0 ? astcBlock.Item2 - restHeight : 0);

            return (newWidth, newHeight);

        }
    }
}
