using Kanvas.Contract;
using Kanvas.Contract.DataClasses;
using Kanvas.Swizzle;
using SixLabors.ImageSharp;

namespace plugin_level5.Common.Image
{
    internal class ImgSwizzle : IImageSwizzle
    {
        private readonly MasterSwizzle _swizzle;

        /// <inheritdoc />
        public int Width { get; }

        /// <inheritdoc />
        public int Height { get; }

        /// <inheritdoc />
        public int MacroTileWidth => _swizzle.MacroTileWidth;

        /// <inheritdoc />
        public int MacroTileHeight => _swizzle.MacroTileHeight;

        public ImgSwizzle(SwizzleOptions options, PlatformType platform)
        {
            Width = options.Size.Width + 7 & ~7;
            Height = options.Size.Height + 7 & ~7;

            _swizzle = GetMasterSwizzle(options, platform);
        }

        /// <inheritdoc />
        public Point Transform(Point point) => Get(point.Y * Width + point.X);

        /// <inheritdoc />
        public Point Get(int pointCount) => _swizzle.Get(pointCount);

        private MasterSwizzle GetMasterSwizzle(SwizzleOptions options, PlatformType platform)
        {
            return platform switch
            {
                PlatformType.Ctr => GetCtrMasterSwizzle(),
                PlatformType.Psp => GetPspMasterSwizzle(options),
                _ => GetDefaultSwizzle(options)
            };
        }

        private MasterSwizzle GetCtrMasterSwizzle()
        {
            return new MasterSwizzle(Width, Point.Empty, [(0, 1), (1, 0), (0, 2), (2, 0), (0, 4), (4, 0)]);
        }

        private MasterSwizzle GetPspMasterSwizzle(SwizzleOptions options)
        {
            return options.EncodingInfo.BitsPerValue switch
            {
                0x04 => new MasterSwizzle(Width, Point.Empty, [(1, 0), (2, 0), (4, 0), (8, 0), (16, 0), (0, 1), (0, 2), (0, 4)]),
                0x08 => new MasterSwizzle(Width, Point.Empty, [(1, 0), (2, 0), (4, 0), (8, 0), (0, 1), (0, 2), (0, 4)]),
                0x10 => new MasterSwizzle(Width, Point.Empty, [(1, 0), (2, 0), (4, 0), (0, 1), (0, 2), (0, 4)]),
                0x20 => new MasterSwizzle(Width, Point.Empty, [(1, 0), (2, 0), (0, 1), (0, 2), (0, 4)]),
                _ => throw new InvalidOperationException("Unknown swizzle for platform 'P'.")
            };
        }

        private MasterSwizzle GetDefaultSwizzle(SwizzleOptions options)
        {
            return options.EncodingInfo.ColorsPerValue > 1 ?
                new MasterSwizzle(Width, Point.Empty, [(1, 0), (2, 0), (0, 1), (0, 2), (4, 0), (8, 0)]) :
                new MasterSwizzle(Width, Point.Empty, [(1, 0), (2, 0), (4, 0), (0, 1), (0, 2), (0, 4)]);
        }
    }
}
