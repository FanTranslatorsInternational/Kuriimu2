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
        public List<PictureBlock> PictureBlocks = new List<PictureBlock>();

        public void Load(Stream input)
        {
            using (var br = new BinaryReaderX(input, true))
            {
                var absoluteBlockPosition = input.Position;
                var chunkHeader = br.ReadStruct<GIMChunk>();

                while (input.Position < input.Length)
                {
                    var pb = new PictureBlock();
                    pb.Load(input);
                    PictureBlocks.Add(pb);
                }
            }
        }

        public void Save(Stream output, List<List<(Bitmap, IImageFormat, ImageBlockMeta, IImageFormat, ImageBlockMeta)>> bmps)
        {
            var headerOffset = output.Position;
            var header = new GIMChunk();
            header.BlockID = 2;
            header.NextBlockRelativeOffset = 0x10;

            output.Position += 0x10;
            var pb = new PictureBlock();
            pb.Save(output, bmps);

            header.BlockSize = (int)(output.Length - headerOffset);
            output.Position = headerOffset;

            using (var bw = new BinaryWriterX(output, true)) bw.WriteStruct(header);
            output.Position = output.Length;
        }
    }

    public sealed class PictureBlock
    {
        public List<ImageBlock> ImageBlocks = new List<ImageBlock>();

        public void Load(Stream input)
        {
            using (var br = new BinaryReaderX(input, true))
            {
                var absoluteBlockPosition = input.Position;
                var chunkHeader = br.ReadStruct<GIMChunk>();

                while (input.Position < input.Length)
                {
                    var ib = new ImageBlock();
                    ib.Load(input);
                    ImageBlocks.Add(ib);
                }
            }
        }

        public void Save(Stream output, List<List<(Bitmap, IImageFormat, ImageBlockMeta, IImageFormat, ImageBlockMeta)>> bmps)
        {
            int index = 0;
            foreach (var picture in bmps)
            {
                bool last = false;
                if (index == bmps.Count - 1)
                    last = true;

                var headerOffset = output.Position;
                var header = new GIMChunk();
                header.BlockID = 3;
                header.NextBlockRelativeOffset = 0x10;

                output.Position += 0x10;
                var ib = new ImageBlock();
                ib.Save(output, picture, last);

                header.BlockSize = (int)(output.Length - headerOffset);
                output.Position = headerOffset;

                using (var bw = new BinaryWriterX(output, true)) bw.WriteStruct(header);
                output.Position = output.Length;

                index++;
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
        private IImageFormat _paletteFormat;
        private ImageBlockMeta _paletteMeta;
        private long _paletteBlockEnd;

        public List<(Bitmap, IImageFormat, ImageBlockMeta, IImageFormat, ImageBlockMeta)> Bitmaps =
            new List<(Bitmap, IImageFormat, ImageBlockMeta, IImageFormat, ImageBlockMeta)>();

        public void Load(Stream input)
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

                    var paletteBlock = new PaletteBlock();
                    paletteBlock.Load(input);
                    _palette = paletteBlock.Palette;
                    _paletteFormat = paletteBlock.Format;
                    _paletteMeta = paletteBlock.ImageMeta;

                    _paletteBlockEnd = input.Position;
                    input.Position = bk;
                }

                (int Width, int Height) = Support.SwizzleAlign(_imageMeta.Width, _imageMeta.Height, _imageMeta.Bpp);

                var settings = new ImageSettings
                {
                    Height = _imageMeta.Height,
                    Width = _imageMeta.Width,
                    Format = Support.Formats[_imageMeta.ImageFormat],
                    Swizzle = _imageMeta.PixelOrder == 1 ? new GIMSwizzle(Width, Height, _imageMeta.Bpp) : null
                };

                if (_paletteUsed)
                    (settings.Format as IPaletteFormat)?.SetPalette(_palette);

                for (var i = 0; i < _imageMeta.LevelCount; i++)
                {
                    var data = br.ReadBytes(Width * Height * _imageMeta.Bpp / 8);
                    Bitmaps.Add((Common.Load(data, settings), Support.Formats[_imageMeta.ImageFormat], _imageMeta, _paletteFormat, _paletteMeta));

                    settings.Height >>= 1;
                    settings.Width >>= 1;
                    settings.Swizzle = _imageMeta.PixelOrder == 1 ? new GIMSwizzle(settings.Width, settings.Height, _imageMeta.Bpp) : null;
                }

                if (_paletteUsed)
                    input.Position = _paletteBlockEnd;
            }
        }

        public void Save(Stream output, List<(Bitmap, IImageFormat, ImageBlockMeta, IImageFormat, ImageBlockMeta)> bmps, bool last)
        {
            int index = 0;
            foreach (var image in bmps)
            {
                bool last2 = false;
                if (index == bmps.Count - 1)
                    last2 = true;

                var headerOffset = output.Position;
                var header = new GIMChunk();
                header.BlockID = 4;

                var meta = image.Item3;
                meta.ImageFormat = ImageFormat.RGBA4444;
                meta.Height = (short)image.Item1.Height;
                meta.Width = (short)image.Item1.Width;

                (int Width, int Height) = Support.SwizzleAlign(meta.Width, meta.Height, meta.Bpp);

                var settings = new ImageSettings
                {
                    Width = meta.Width,
                    Height = meta.Height,
                    Swizzle = meta.PixelOrder == 1 ? new GIMSwizzle(Width, Height, meta.Bpp) : null,
                    Format = Support.Formats[meta.ImageFormat]
                };
                var data = Common.Save(image.Item1, settings);

                meta.PixelEnd = data.Length + meta.PixelStart;
                if (!(last2 & last /*& !(image.Item2 is IPaletteFormat)*/))
                    header.NextBlockRelativeOffset = (0x10 + 0x40 + data.Length + 0xF) & ~0xF;
                using (var bw = new BinaryWriterX(output, true))
                {
                    bw.WriteStruct(header);
                    bw.WriteStruct(meta);
                    bw.Write(data);
                    bw.WriteAlignment();
                }

                //if (image.Item2 is IPaletteFormat paletteFormat)
                //{
                //    var pab = new PaletteBlock();
                //    pab.Save(output, paletteFormat.GetPalette(), image.Item4, image.Item5, last2 & last);
                //}

                output.Position = output.Length;

                index++;
            }
        }
    }

    public sealed class PaletteBlock
    {
        private GIMChunk _chunkHeader;
        public ImageBlockMeta ImageMeta;
        private long _absoluteBlockPosition;

        public List<Color> Palette;
        public IImageFormat Format;

        public void Load(Stream input)
        {
            using (var br = new BinaryReaderX(input, true))
            {
                _absoluteBlockPosition = input.Position;
                _chunkHeader = br.ReadStruct<GIMChunk>();
                ImageMeta = br.ReadStruct<ImageBlockMeta>();

                Format = Support.Formats[ImageMeta.ImageFormat];
                Palette = Format.Load(br.ReadBytes(ImageMeta.Width * ImageMeta.Height * ImageMeta.Bpp / 8)).ToList();
            }
        }

        public void Save(Stream output, IEnumerable<Color> palette, IImageFormat format, ImageBlockMeta meta, bool last)
        {
            var headerOffset = output.Position;
            var header = new GIMChunk();
            header.BlockID = 5;

            meta.PixelOrder = 0;
            var dataP = format.Save(palette);

            header.BlockSize = (0x10 + 0x40 + dataP.Length + 0xF) & ~0xF;
            if (!last)
                header.NextBlockRelativeOffset = (int)(output.Length - headerOffset);

            using (var bw = new BinaryWriterX(output, true))
            {
                bw.WriteStruct(header);
                bw.WriteStruct(meta);
                bw.Write(dataP);
                bw.WriteAlignment();
            }
        }
    }

    public sealed class ImageBlockMeta
    {
        public short MetaLength = 0x30;
        public short Unk1 = 0;
        public ImageFormat ImageFormat;
        public short PixelOrder;
        public short Width;
        public short Height;
        public short Bpp;
        public short PitchAlign = 0x10;
        public short HeightAlign = 8;
        public short Unk2 = 2;
        public int Unk3 = 0;
        public int IndexStart = 0x30;
        public int PixelStart = 0x40;
        public int PixelEnd;

        public int PlaneMask = 0;
        public short LevelType;
        public short LevelCount = 1;
        public short FrameType = 3;
        public short FrameCount = 1;
        public int Frame_n_Offset = 0x40;

        [FixedLength(0xC)]
        public byte[] Padding = new byte[0xC];
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

        public static (int, int) SwizzleAlign(int Width, int Height, int Bpp)
        {
            int Align(int input, int align) => input + (align - 1) & ~(align - 1);

            switch (Bpp)
            {
                case 4:
                    return (Align(Width, 32), Align(Height, 8));

                case 8:
                    return (Align(Width, 16), Align(Height, 8));

                default:
                    return (0, 0);
            }
        }
    }

    public sealed class GIMSwizzle : IImageSwizzle
    {
        MasterSwizzle _master;

        public GIMSwizzle(int Width, int Height, int Bpp)
        {
            this.Width = Width;
            this.Height = Height;

            List<(int, int)> bitField = new List<(int, int)> { (0, 0) };
            switch (Bpp)
            {
                case 4:
                    bitField = new List<(int, int)> { (1, 0), (2, 0), (4, 0), (8, 0), (16, 0), (0, 1), (0, 2), (0, 4) };
                    break;

                case 8:
                    bitField = new List<(int, int)> { (1, 0), (2, 0), (4, 0), (8, 0), (0, 1), (0, 2), (0, 4) };
                    break;
            }

            _master = new MasterSwizzle(Width, new Point(0, 0), bitField);
        }

        public int Width { get; }

        public int Height { get; }

        public Point Get(Point point) => _master.Get(point.X + point.Y * Width);
    }

    public sealed class GIMChunk
    {
        public short BlockID;
        public short Unk1 = 0;
        public int BlockSize; // With Header
        public int NextBlockRelativeOffset;
        public int BlockDataOffset = 0x10;
    }
}
