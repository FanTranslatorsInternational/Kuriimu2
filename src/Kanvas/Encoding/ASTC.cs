using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using Kanvas.Encoding.BlockCompressions.ASTC;
using Kanvas.Encoding.BlockCompressions.ASTC.KTX;
using Kanvas.Encoding.BlockCompressions.ASTC.Models;
using Kontract.Kanvas;
using Kontract.Models.IO;

namespace Kanvas.Encoding
{
    /// <summary>
    /// Defines the ASTC encoding.
    /// </summary>
    public class ASTC : IColorEncodingKnownDimensions
    {
        private readonly int _xDim;
        private readonly int _yDim;
        private readonly int _zDim;
        private readonly BlockMode _blockMode;

        /// <inheritdoc cref="IColorEncoding.BitDepth"/>
        public int BitDepth { get; }

        /// <summary>
        /// The number of bits one block contains of.
        /// </summary>
        public int BlockBitDepth { get; }

        /// <inheritdoc cref="IColorEncoding.FormatName"/>
        public string FormatName { get; }

        /// <inheritdoc cref="IColorEncoding.IsBlockCompression"/>
        public bool IsBlockCompression => true;

        /// <inheritdoc cref="IColorEncodingKnownDimensions.Width"/>
        public int Width { private get; set; } = -1;

        /// <inheritdoc cref="IColorEncodingKnownDimensions.Height"/>
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

            //File.Delete("tmp.ktx");

            return colors;
        }

        private void CreateTempASTCFile(string astcFile, byte[] texData)
        {
            var astc = File.Create(astcFile);

            using (var bw = new BinaryWriter(astc, System.Text.Encoding.ASCII, true))
            {
                bw.Write(0x5CA1AB13);   // magic val
                bw.Write((byte)_xDim);
                bw.Write((byte)_yDim);
                bw.Write((byte)_zDim);
                bw.Write(Kanvas.Support.Convert.ToByteArray(Width, 3, ByteOrder));
                bw.Write(Kanvas.Support.Convert.ToByteArray(Height, 3, ByteOrder));
                bw.Write(Kanvas.Support.Convert.ToByteArray(1, 3, ByteOrder));
                bw.Write(texData);
            }

            astc.Dispose();
            astc.Close();
        }

        public byte[] Save(IEnumerable<Color> colors)
        {
            if (Width <= 0 || Height <= 0)
                throw new InvalidDataException("Height and Width has to be set for ASTC.");

            CreateTempPNG("tmp.png", colors);

            var wrapper = new ASTCContext();
            var result = wrapper.Encode("tmp.png", "tmp.astc", _blockMode);
            if (result == ConvertImageResult.Error)
                return null;

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
                    if (index / 4 == Width * Height)
                        break;
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
