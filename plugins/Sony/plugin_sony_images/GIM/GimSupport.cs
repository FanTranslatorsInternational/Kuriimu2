using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using Kanvas;
using Kanvas.Format;
using Kanvas.Interface;
using Kanvas.Palette;
using Kanvas.Swizzle;
using Komponent.IO;

namespace plugin_sony_images.GIM
{
    public sealed class RootBlock
    {
        private GIMChunk _chunkHeader;
        private long _absoluteBlockPosition;

        public List<PictureBlock> PictureBlocks;

        public RootBlock(Stream input)
        {
            using (var br = new BinaryReaderX(input, true))
            {
                _absoluteBlockPosition = input.Position;
                _chunkHeader = br.ReadStruct<GIMChunk>();

                PictureBlocks = new List<PictureBlock>();
                while (input.Position < input.Length)
                {
                    PictureBlocks.Add(new PictureBlock(input));
                }
            }
        }
    }

    public sealed class PictureBlock
    {
        private GIMChunk _chunkHeader;
        private long _absoluteBlockPosition;

        public List<ImageBlock> ImageBlocks;

        public PictureBlock(Stream input)
        {
            using (var br = new BinaryReaderX(input, true))
            {
                _absoluteBlockPosition = input.Position;
                _chunkHeader = br.ReadStruct<GIMChunk>();

                ImageBlocks = new List<ImageBlock>();
                while (input.Position < input.Length)
                {
                    ImageBlocks.Add(new ImageBlock(input));
                }
            }
        }
    }

    public sealed class ImageBlock
    {
        private GIMChunk _chunkHeader;
        private ImageBlockMeta _imageMeta;
        private long _absoluteBlockPosition;

        private bool _paletteUsed;
        private List<Color> _palette;
        private long _paletteBlockEnd;

        public List<Bitmap> Bitmaps = new List<Bitmap>();

        public ImageBlock(Stream input)
        {
            using (var br = new BinaryReaderX(input, true))
            {
                _absoluteBlockPosition = input.Position;
                _chunkHeader = br.ReadStruct<GIMChunk>();

                _imageMeta = br.ReadStruct<ImageBlockMeta>();

                //Read Palette if needed
                if (_imageMeta.ImageFormat >= ImageFormat.Palette_4 && _imageMeta.ImageFormat <= ImageFormat.Palette_32)
                {
                    _paletteUsed = true;

                    var bk = input.Position;
                    input.Position = _absoluteBlockPosition + _chunkHeader.NextBlockRelativeOffset;

                    var paletteBlock = new PaletteBlock(input);
                    _palette = paletteBlock.Palette;

                    _paletteBlockEnd = input.Position;
                    input.Position = bk;
                }

                var settings = new ImageSettings
                {
                    Height = _imageMeta.Height,
                    Width = _imageMeta.Width,
                    Format = Support.Formats[_imageMeta.ImageFormat],
                    Swizzle = _imageMeta.PixelOrder == 1 ? new GIMSwizzle(_imageMeta.Width, _imageMeta.Height, _imageMeta.PitchAlign, _imageMeta.HeightAlign) : null
                };

                if (_paletteUsed)
                    (settings.Format as IPaletteFormat)?.SetPalette(_palette);

                for (var i = 0; i < _imageMeta.LevelCount; i++)
                {
                    var data = br.ReadBytes(settings.Width * settings.Height * _imageMeta.Bpp / 8);
                    Bitmaps.Add(Common.Load(data, settings));

                    settings.Height >>= 1;
                    settings.Width >>= 1;
                    settings.Swizzle = _imageMeta.PixelOrder == 1 ? new GIMSwizzle(settings.Width, settings.Height, _imageMeta.PitchAlign, _imageMeta.HeightAlign) : null;
                }

                if (_paletteUsed)
                    input.Position = _paletteBlockEnd;
            }
        }
    }

    public sealed class PaletteBlock
    {
        private GIMChunk _chunkHeader;
        private ImageBlockMeta _imageMeta;
        private long _absoluteBlockPosition;

        public List<Color> Palette;

        public PaletteBlock(Stream input)
        {
            using (var br = new BinaryReaderX(input, true))
            {
                _absoluteBlockPosition = input.Position;
                _chunkHeader = br.ReadStruct<GIMChunk>();
                _imageMeta = br.ReadStruct<ImageBlockMeta>();

                Palette = Support.Formats[_imageMeta.ImageFormat].Load(br.ReadBytes(_imageMeta.Width * _imageMeta.Height * _imageMeta.Bpp / 8)).ToList();
            }
        }
    }

    public sealed class ImageBlockMeta
    {
        public short MetaLength;
        public short Unk1;
        public ImageFormat ImageFormat;
        public short PixelOrder;
        public short Width;
        public short Height;
        public short Bpp;
        public short PitchAlign;
        public short HeightAlign;
        public short Unk2;
        public int Unk3;
        public int IndexStart;
        public int PixelStart;
        public int PixelEnd;

        public int PlaneMask;
        public short LevelType;
        public short LevelCount;
        public short FrameType;
        public short FrameCount;
        public int Frame_n_Offset;

        [FixedLength(0xC)]
        public byte[] Padding;
    }

    public enum ImageFormat : short
    {
        RGB565,
        RGBA5551,
        RGBA4444,
        RGBA8888,
        Palette_4,
        Palette_8,
        Palette_16,
        Palette_32,
        DXT1,
        DXT3,
        DXT5
    }

    public sealed class Support
    {
        public static Dictionary<ImageFormat, IImageFormat> Formats = new Dictionary<ImageFormat, IImageFormat>
        {
            [ImageFormat.RGB565] = new RGBA(5, 6, 5, 0, true, true),
            [ImageFormat.RGBA5551] = new RGBA(5, 5, 5, 1, true, true),
            [ImageFormat.RGBA4444] = new RGBA(4, 4, 4, 4, true, true),
            [ImageFormat.RGBA8888] = new RGBA(8, 8, 8, 8, true, true),
            [ImageFormat.Palette_4] = new Palette(4),
            [ImageFormat.Palette_8] = new Palette(8),
            //[6] = new AI(8,8),    ??
            //[7] = new AI(24,8),   ??
            [ImageFormat.DXT1] = new DXT(DXT.Format.DXT1),
            [ImageFormat.DXT3] = new DXT(DXT.Format.DXT3),
            [ImageFormat.DXT5] = new DXT(DXT.Format.DXT5),
        };

        public static int SwizzleAlignWidth(int Width)
        {
            return 0;
        }
        public static int SwizzleAlignHeight(int Height)
        {
            return 0;
        }
    }

    public sealed class GIMSwizzle : IImageSwizzle
    {
        MasterSwizzle _master;

        public GIMSwizzle(int Width, int Height, int WidthAlign, int HeightAlign)
        {
            this.Width = Width;
            this.Height = Height;

            List<(int, int)> bitField = new List<(int, int)> { (0, 0) };
            if (Width >= 256 || Height >= 256)
                bitField = new List<(int, int)> { (1, 0), (2, 0), (4, 0), (8, 0), (0, 1), (0, 2), (0, 4) };
            else
                bitField = new List<(int, int)> { (1, 0), (2, 0), (4, 0), (8, 0), (16, 0), (0, 1), (0, 2), (0, 4) };
            _master = new MasterSwizzle(Width, new Point(0, 0), bitField);
        }

        public int Width { get; }

        public int Height { get; }

        public Point Get(Point point) => _master.Get(point.X + point.Y * Width);
    }

    public sealed class GIMChunk
    {
        public short BlockID;
        public short Unk1;
        public int BlockSize; // With Header
        public int NextBlockRelativeOffset;
        public int BlockDataOffset;
    }
}
