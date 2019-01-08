using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using Kanvas.Support;
using Komponent.IO;
using Kontract.Interfaces.Font;

namespace plugin_valkyria_chronicles.SFNT
{
    public sealed class SFNT
    {
        /// <summary>
        /// The size in bytes of the SFNT Header.
        /// </summary>
        private const int SfntHeaderSize = 0x60;

        /// <summary>
        /// The list of images in the font.
        /// </summary>
        public List<Bitmap> Images { get; set; } = new List<Bitmap>();

        /// <summary>
        /// The list of characters in the font.
        /// </summary>
        public List<FontCharacter2> Characters { get; set; } = new List<FontCharacter2>();

        #region InstanceData

        private PacketHeaderX _packetHeader;
        private SFNTHeader _sfntHeader;
        private PacketHeaderX _sfntFooter;
        private List<(PacketHeaderX Header, byte[] Data)> _imageBlocks = new List<(PacketHeaderX Header, byte[] Data)>();
        private List<(PacketHeader Header, byte[] Data)> _dataBlocks = new List<(PacketHeader Header, byte[] Data)>();

        #endregion

        /// <summary>
        /// Read an SFNT file into memory.
        /// </summary>
        /// <param name="input">A readable stream of an SFNT file.</param>
        public SFNT(Stream input)
        {
            using (var br = new BinaryReaderX(input))
            {
                // Packet Header
                _packetHeader = br.ReadStruct<PacketHeaderX>();

                // SFNT Header
                _sfntHeader = br.ReadStruct<SFNTHeader>();
                var offsets = br.ReadMultiple<int>(_sfntHeader.EntryCount);

                // Blocks
                foreach (var offset in offsets)
                {
                    br.BaseStream.Position = offset;
                    switch (br.PeekString())
                    {
                        case "MFNT":
                            var packetHeaderX = br.ReadStruct<PacketHeaderX>();
                            _imageBlocks.Add((packetHeaderX, br.ReadBytes(packetHeaderX.PacketSize)));
                            break;
                        case "MFGT":
                        case "HFPR":
                            var packetHeader = br.ReadStruct<PacketHeader>();
                            _dataBlocks.Add((packetHeader, br.ReadBytes(packetHeader.PacketSize)));
                            break;
                    }
                }

                // Images
                foreach (var (header, data) in _imageBlocks)
                {
                    const int bitDepth = 2;
                    const int pixelsPerByte = 8 / bitDepth;
                    const int width = 16;
                    switch (header.Magic)
                    {
                        case "MFNT":
                            // Temporary until we get support for 2bpp and something like BitDepthOrder in Kanvas
                            var bmp = new Bitmap(width, data.Length * pixelsPerByte / width);
                            int x = 0, y = 0;

                            foreach (var b in data)
                            {
                                for (var i = 0; i < pixelsPerByte; i++)
                                {
                                    var l = Helper.ChangeBitDepth((b >> 6 - bitDepth * i) & 0x3, bitDepth, 8);
                                    bmp.SetPixel(x++, y, Color.FromArgb(255, l, l, l));
                                }

                                if (x != bmp.Width) continue;
                                x = 0;
                                y++;
                            }

                            Images.Add(bmp);
                            break;
                        case "MFGT":
                            // The character list.
                            break;
                        case "HFPR":
                            // The variable width character data.
                            break;
                    }
                }

                // Characters
                List<int> asciiWidths = null;

                using (var hbr = new BinaryReaderX(new MemoryStream(_dataBlocks[1].Data)))
                {
                    asciiWidths = hbr.ReadMultiple<byte>((int)hbr.BaseStream.Length).Select(x => (int)x).ToList();
                }

                //var mfgt = new MemoryStream(_dataBlocks[0].Data);

                // ASCII
                const int height = 16;
                for (uint i = 0; i < asciiWidths.Count; i++)
                {
                    // Glyph
                    var width = asciiWidths[(int)i];

                    var glyph = new Bitmap(Math.Max(width, 1), height, PixelFormat.Format32bppArgb);
                    var gfx = Graphics.FromImage(glyph);
                    gfx.DrawImage(Images[0],
                        new[] {
                            new PointF(0, 0),
                            new PointF(width, 0),
                            new PointF(0, height)
                        },
                        new RectangleF(0, i * height, width, height),
                        GraphicsUnit.Pixel);

                    // Character
                    Characters.Add(new FontCharacter2
                    {
                        Character = i + ' ',
                        WidthInfo = new CharWidthInfo
                        {
                            CharWidth = width,
                            GlyphWidth = width,
                            Left = 0
                        },
                        GlyphWidth = width,
                        GlyphHeight = height,
                        Glyph = glyph
                    });
                }

                // SFNT Footer
                _sfntFooter = br.ReadStruct<PacketHeaderX>();
            }
        }

        /// <summary>
        /// Write an SFNT file to disk.
        /// </summary>
        /// <param name="output">A writable stream of an SFNT file.</param>
        public void Save(Stream output)
        {
            using (var bw = new BinaryWriterX(output))
            {
                throw new NotImplementedException();
            }
        }
    }
}
