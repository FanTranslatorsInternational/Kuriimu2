using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kanvas.Interface;
using System.Drawing;
using Komponent.IO;
using System.IO;
using Kanvas.Support.ASTC;
using Komponent.IO.Attributes;

namespace Kanvas.Format
{
    public class ASTC : IImageFormatKnownDimensions
    {
        public int BitDepth { get; set; }
        public int BlockBitDepth { get; set; }

        public string FormatName { get; set; }

        public bool IsBlockCompression { get => true; }
        public int Width { private get; set; } = -1;
        public int Height { private get; set; } = -1;

        int _xdim;
        int _ydim;
        int _zdim;
        BlockMode _blockMode;

        ByteOrder _byteOrder;

        public ASTC(int xdim, int ydim, ByteOrder byteOrder = ByteOrder.LittleEndian) : this(xdim, ydim, 1, byteOrder)
        {
        }

        public ASTC(int xdim, int ydim, int zdim, ByteOrder byteOrder = ByteOrder.LittleEndian)
        {
            BitDepth = -1;
            BlockBitDepth = 128;

            if (!Enum.TryParse($"ASTC{xdim}x{ydim}" + ((zdim > 1) ? $"x{zdim}" : ""), out _blockMode))
                throw new InvalidDataException($"Invalid ASTC BlockMode ASTC{xdim}x{ydim}" + ((zdim > 1) ? $"x{zdim}" : ""));

            _xdim = xdim;
            _ydim = ydim;
            _zdim = zdim;

            FormatName = _blockMode.ToString();

            _byteOrder = byteOrder;
        }

        private class ASTCFileHeader
        {
            public uint magic = 0x5CA1AB13;
            public byte xdim;
            public byte ydim;
            public byte zdim;
            [FixedLength(3)]
            public byte[] width;
            [FixedLength(3)]
            public byte[] height;
            [FixedLength(3)]
            public byte[] depth;
        }

        public IEnumerable<Color> Load(byte[] tex)
        {
            if (Width < 0 || Height < 0)
                throw new InvalidDataException("Height and Width has to be set for ASTC.");

            CreateTempASTCFile("tmp.astc", tex);

            var wrapper = new ASTCContext();
            wrapper.Decode("tmp.astc", "tmp.ktx", _blockMode);
            File.Delete("tmp.astc");

            var ktx = new KTX("tmp.ktx");
            var colors = ktx.GetImageColors().Reverse().ToList();
            ktx.Dispose();
            File.Delete("tmp.ktx");

            return colors;
        }

        private void CreateTempASTCFile(string astcFile, byte[] texData)
        {
            var header = new ASTCFileHeader
            {
                xdim = (byte)_xdim,
                ydim = (byte)_ydim,
                zdim = (byte)_zdim,
                width = ConvertInt32ToBA(Width, 3),
                height = ConvertInt32ToBA(Height, 3),
                depth = ConvertInt32ToBA(1, 3)
            };

            var ms = new MemoryStream();
            using (var bw = new BinaryWriterX(ms, true))
            {
                bw.WriteStruct(header);
                bw.Write(texData);
            }

            File.WriteAllBytes(astcFile, ms.ToArray());
        }

        private byte[] ConvertInt32ToBA(int input, int count)
        {
            var ret = new byte[4];
            for (int i = 0; i < 4; i++)
                ret[i] = (byte)((input >> (i * 8)) & 0xFF);
            return ret.Take(count).ToArray();
        }

        public byte[] Save(IEnumerable<Color> colors)
        {
            if (Width < 0 || Height < 0)
                throw new InvalidDataException("Height and Width has to be set for ASTC.");

            CreateTempPNG("tmp.png", colors);

            var wrapper = new ASTCContext();
            wrapper.Encode("tmp.png", "tmp.astc", _blockMode);
            File.Delete("tmp.png");

            byte[] encodedData = null;
            using (var br = new BinaryReaderX(File.OpenRead("tmp.astc")))
            {
                br.ReadStruct<ASTCFileHeader>();
                encodedData = br.ReadBytes((int)(br.BaseStream.Length - br.BaseStream.Position));
            }
            File.Delete("tmp.astc");

            return encodedData;
        }

        private void CreateTempPNG(string pngFile, IEnumerable<Color> colors)
        {
            var bitmap = new Bitmap(Width, Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            var data = bitmap.LockBits(new Rectangle(0, 0, Width, Height), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            unsafe
            {
                var pointer = (byte*)data.Scan0;
                var index = 0;
                foreach (var color in colors)
                {
                    pointer[index++] = color.B;
                    pointer[index++] = color.G;
                    pointer[index++] = color.R;
                    pointer[index++] = color.A;
                }
            }
            bitmap.UnlockBits(data);

            bitmap.Save(pngFile);
        }
    }
}
