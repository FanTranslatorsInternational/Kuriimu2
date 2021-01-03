using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Kanvas.Encoding.BlockCompressions.ASTC;
using Kanvas.Encoding.BlockCompressions.ASTC.Models;
using Kanvas.Encoding.BlockCompressions.ASTC_CS;
using Komponent.IO;
using Komponent.Utilities;
using Kontract.Kanvas;
using Kontract.Models.IO;

namespace Kanvas.Encoding
{
    /// <summary>
    /// Defines the ASTC encoding.
    /// </summary>
    public class ASTC : IColorEncodingKnownDimensions
    {
        private readonly AstcBlockDecoder _decoder;

        private readonly int _xDim;
        private readonly int _yDim;
        private readonly int _zDim;
        private readonly BlockMode _blockMode;

        /// <inheritdoc cref="IColorEncoding.BitDepth"/>
        public int BitDepth { get; }

        public int BitsPerValue { get; }

        public int ColorsPerValue { get; }

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

        public ASTC(int xdim, int ydim) : this(xdim, ydim, 1)
        {
        }

        public ASTC(int xdim, int ydim, int zdim)
        {
            _xDim = xdim;
            _yDim = ydim;
            _zDim = zdim;
            _decoder = new AstcBlockDecoder(xdim, ydim, zdim);

            BitDepth = -1;
            BlockBitDepth = BitsPerValue = 128;
            ColorsPerValue = xdim * ydim * zdim;

            FormatName = $"ASTC{xdim}x{ydim}" + (zdim > 1 ? $"x{zdim}" : "");
            if (!Enum.TryParse(FormatName, out _blockMode))
                throw new InvalidDataException($"Block mode {FormatName} is not supported.");
        }

        public IEnumerable<Color> Load(byte[] tex, int taskCount)
        {
            // TODO: Use block compression base class

            using (var br = new BinaryReaderX(new MemoryStream(tex)))
            {
                while (br.BaseStream.Position < br.BaseStream.Length)
                    foreach (var color in _decoder.DecodeBlocks(br.ReadBytes(16)))
                        yield return color;
            }

            //if (Width <= 0 || Height <= 0)
            //    throw new InvalidDataException("Height and Width has to be set for ASTC.");

            //CreateTempASTCFile("tmp.astc", tex);

            //var wrapper = new ASTCContext();
            //wrapper.Decode("tmp.astc", "tmp.ktx", _blockMode);
            //File.Delete("tmp.astc");

            //var ktx = new KTXWrapper("tmp.ktx");
            //var colors = ktx.GetImageColors().Reverse().ToList();
            //ktx.Dispose();

            ////File.Delete("tmp.ktx");

            //return colors;
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
                bw.Write(Conversion.ToByteArray(Width, 3, ByteOrder.LittleEndian));
                bw.Write(Conversion.ToByteArray(Height, 3, ByteOrder.LittleEndian));
                bw.Write(Conversion.ToByteArray(1, 3, ByteOrder.LittleEndian));
                bw.Write(texData);
            }

            astc.Dispose();
            astc.Close();
        }

        public byte[] Save(IEnumerable<Color> colors, int taskCount)
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
