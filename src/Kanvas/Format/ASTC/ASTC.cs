using System;
using System.Collections.Generic;
using System.Linq;
using Kanvas.Interface;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using Kanvas.Format.ASTC.KTX;
using Kanvas.Format.ASTC.Models;
using Kanvas.Models;
using Convert = Kanvas.Support.Convert;

namespace Kanvas.Format.ASTC
{
    /// <summary>
    /// Defines the ASTC encoding.
    /// </summary>
    public class ASTC : IColorTranscodingKnownDimensions
    {
        private readonly int _xDim;
        private readonly int _yDim;
        private readonly int _zDim;
        private readonly BlockMode _blockMode;

        /// <inheritdoc cref="IColorTranscoding.BitDepth"/>
        public int BitDepth { get; }

        /// <summary>
        /// The number of bits one block contains of.
        /// </summary>
        public int BlockBitDepth { get; }

        /// <inheritdoc cref="IColorTranscoding.FormatName"/>
        public string FormatName { get; }

        /// <inheritdoc cref="IColorTranscoding.IsBlockCompression"/>
        public bool IsBlockCompression => true;

        /// <inheritdoc cref="IColorTranscodingKnownDimensions.Width"/>
        public int Width { private get; set; } = -1;

        /// <inheritdoc cref="IColorTranscodingKnownDimensions.Height"/>
        public int Height { private get; set; } = -1;

        /// <summary>
        /// Byte order to use to read the values.
        /// </summary>
        public ByteOrder ByteOrder { get; set; } = ByteOrder.LittleEndian;

        public ASTC(int xdim, int ydim) : this(xdim, ydim, 1)
        {
        }

        public ASTC(int xdim, int ydim, int zdim)
        {
            BitDepth = -1;
            BlockBitDepth = 128;

            var modeName = CreateName(xdim, ydim, zdim);
            if (!Enum.TryParse(modeName, out _blockMode))
                throw new InvalidDataException($"Block mode {modeName} is not supported.");

            _xDim = xdim;
            _yDim = ydim;
            _zDim = zdim;

            FormatName = modeName;
        }

        private string CreateName(int xdim, int ydim, int zdim)
        {
            return $"ASTC{xdim}x{ydim}" + ((zdim > 1) ? $"x{zdim}" : "");
        }

        public IEnumerable<Color> Load(byte[] tex)
        {
            if (Width <= 0 || Height <= 0)
                throw new InvalidDataException("Height and Width has to be set for ASTC.");

            CreateTempASTCFile("tmp.astc", tex);

            var wrapper = new ASTCContext();
            wrapper.Decode("tmp.astc", "tmp.ktx", _blockMode);
            File.Delete("tmp.astc");

            var ktx = new KTXWrapper("tmp.ktx");
            var colors = ktx.GetImageColors().Reverse().ToList();
            ktx.Dispose();

            File.Delete("tmp.ktx");

            return colors;
        }

        private void CreateTempASTCFile(string astcFile, byte[] texData)
        {
            using (var bw = new BinaryWriter(File.Create(astcFile), Encoding.ASCII, true))
            {
                bw.Write((byte)_xDim);
                bw.Write((byte)_yDim);
                bw.Write((byte)_zDim);
                bw.Write(Convert.ToByteArray(Width, 3, ByteOrder));
                bw.Write(Convert.ToByteArray(Height, 3, ByteOrder));
                bw.Write(Convert.ToByteArray(1, 3, ByteOrder));
                bw.Write(texData);
            }
        }

        public byte[] Save(IEnumerable<Color> colors)
        {
            if (Width <= 0 || Height <= 0)
                throw new InvalidDataException("Height and Width has to be set for ASTC.");

            CreateTempPNG("tmp.png", colors);

            var wrapper = new ASTCContext();
            wrapper.Encode("tmp.png", "tmp.astc", _blockMode);
            File.Delete("tmp.png");

            byte[] encodedData;
            using (var br = new BinaryReader(File.OpenRead("tmp.astc")))
            {
                br.BaseStream.Position += 12;
                encodedData = br.ReadBytes((int)(br.BaseStream.Length - br.BaseStream.Position));
            }
            File.Delete("tmp.astc");

            return encodedData;
        }

        private void CreateTempPNG(string pngFile, IEnumerable<Color> colors)
        {
            var bitmap = new Bitmap(Width, Height);
            var data = bitmap.LockBits(new Rectangle(0, 0, Width, Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
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
