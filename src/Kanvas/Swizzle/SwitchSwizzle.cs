using System;
using System.Collections.Generic;
using System.Linq;
using Kanvas.Interface;
using System.Drawing;
using Kanvas.Swizzle.Models;

namespace Kanvas.Swizzle
{
    /// <summary>
    /// Defines the swizzle used on the Nintendo Switch.
    /// </summary>
    public class SwitchSwizzle : IImageSwizzle
    {
        private readonly MasterSwizzle _swizzle;

        private const int BlockMaxSize = 512;
        private const int RegularMaxSize = 128;

        private readonly Dictionary<SwitchFormat, (int, int)> _astcBlock = new Dictionary<SwitchFormat, (int, int)>
        {
            [SwitchFormat.ASTC4x4] = (4, 4),
            [SwitchFormat.ASTC5x4] = (5, 4),
            [SwitchFormat.ASTC5x5] = (5, 5),
            [SwitchFormat.ASTC6x5] = (6, 5),
            [SwitchFormat.ASTC6x6] = (6, 6),
            [SwitchFormat.ASTC8x5] = (8, 5),
            [SwitchFormat.ASTC8x6] = (8, 6),
            [SwitchFormat.ASTC8x8] = (8, 8),
            [SwitchFormat.ASTC10x5] = (10, 5),
            [SwitchFormat.ASTC10x6] = (10, 6),
            [SwitchFormat.ASTC10x8] = (10, 8),
            [SwitchFormat.ASTC10x10] = (10, 10),
            [SwitchFormat.ASTC12x10] = (12, 10),
            [SwitchFormat.ASTC12x12] = (12, 12)
        };

        private readonly Dictionary<int, (int, int)[]> _coordsBlock = new Dictionary<int, (int, int)[]>
        {
            [4] = new[] { (1, 0), (2, 0), (0, 1), (0, 2), (4, 0), (0, 4), (8, 0), (0, 8), (0, 16), (16, 0) },
            [8] = new[] { (1, 0), (2, 0), (0, 1), (0, 2), (0, 4), (4, 0), (0, 8), (0, 16), (8, 0) }
        };

        private readonly Dictionary<int, (int, int)[]> _coordsRegular = new Dictionary<int, (int, int)[]>
        {
            [32] = new[] { (1, 0), (2, 0), (0, 1), (4, 0), (0, 2), (0, 4), (8, 0), (0, 8), (0, 16) },
        };

        /// <inheritdoc cref="IImageSwizzle.Width"/>
        public int Width { get; }

        /// <inheritdoc cref="IImageSwizzle.Height"/>
        public int Height { get; }

        /// <summary>
        /// Creates a new instance of <see cref="SwitchSwizzle"/>.
        /// </summary>
        /// <param name="width">The width of the image to swizzle.</param>
        /// <param name="height">The height of the image to swizzle.</param>
        /// <param name="bitDepth">Bit depth of the components.</param>
        /// <param name="format">The format of the image to be swizzled.</param>
        /// <param name="toPowerOf2">Should the dimensions be padded to a power of 2.</param>
        public SwitchSwizzle(int width, int height, int bitDepth, SwitchFormat format, bool toPowerOf2)
        {
            Width = (toPowerOf2) ? 2 << (int)Math.Log(width - 1, 2) : width;
            Height = (toPowerOf2) ? 2 << (int)Math.Log(height - 1, 2) : height;

            (Width, Height) = PadDimensions(Width, Height, format);

            var bitField = GetBitField(bitDepth, IsBlockBased(format));
            _swizzle = (bitField == null) ? new LinearSwizzle(Width, Height).Swizzle : new MasterSwizzle(Width, new Point(0, 0), bitField);
        }

        /// <inheritdoc cref="IImageSwizzle.Get(Point)"/>
        public Point Get(Point point) => _swizzle.Get(point.Y * Width + point.X);

        private static bool IsBlockBased(SwitchFormat format) =>
            format == SwitchFormat.DXT1 || format == SwitchFormat.DXT3 || format == SwitchFormat.DXT5 ||
            format == SwitchFormat.ATI1 || format == SwitchFormat.ATI2 ||
            format == SwitchFormat.BC6H || format == SwitchFormat.BC7;

        private (int, int) PadDimensions(int width, int height, SwitchFormat format)
        {
            switch (format)
            {
                case SwitchFormat.DXT1:
                case SwitchFormat.DXT3:
                case SwitchFormat.DXT5:
                case SwitchFormat.ATI1:
                case SwitchFormat.ATI2:
                case SwitchFormat.BC6H:
                case SwitchFormat.BC7:
                    return ((width + 3) & ~3, (height + 3) & ~3);
                case SwitchFormat.ASTC4x4:
                case SwitchFormat.ASTC5x4:
                case SwitchFormat.ASTC5x5:
                case SwitchFormat.ASTC6x5:
                case SwitchFormat.ASTC6x6:
                case SwitchFormat.ASTC8x5:
                case SwitchFormat.ASTC8x6:
                case SwitchFormat.ASTC8x8:
                case SwitchFormat.ASTC10x5:
                case SwitchFormat.ASTC10x6:
                case SwitchFormat.ASTC10x8:
                case SwitchFormat.ASTC10x10:
                case SwitchFormat.ASTC12x10:
                case SwitchFormat.ASTC12x12:
                    var restWidth = width % _astcBlock[format].Item1;
                    var newWidth = width + (restWidth != 0 ? _astcBlock[format].Item1 - restWidth : 0);

                    var restHeight = height % _astcBlock[format].Item2;
                    var newHeight = height + (restHeight != 0 ? _astcBlock[format].Item2 - restHeight : 0);

                    return (newWidth, newHeight);
                default:
                    return (width, height);
            }
        }

        private (int, int)[] GetBitField(int bpp, bool isBlockBased)
        {
            List<(int, int)> bitField;
            if (isBlockBased)
            {
                bitField = (_coordsBlock.ContainsKey(bpp) ? _coordsBlock[bpp].ToList() : null);
                if (bitField == null) return null;
                for (var i = 32; i < Math.Min(Height, BlockMaxSize); i *= 2)
                    bitField.Add((0, i));
            }
            else
            {
                bitField = (_coordsRegular.ContainsKey(bpp) ? _coordsRegular[bpp].ToList() : null);
                if (bitField == null) return null;
                for (var i = 32; i < Math.Min(Height, RegularMaxSize); i *= 2)
                    bitField.Add((0, i));
            }

            return bitField.ToArray();
        }
    }
}
