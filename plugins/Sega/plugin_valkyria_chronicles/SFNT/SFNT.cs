using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Kanvas.Support;
using Komponent.IO;

namespace plugin_valkyria_chronicles.SFNT
{
    public sealed class SFNT
    {
        /// <summary>
        /// The size in bytes of the SFNT Header.
        /// </summary>
        private const int SfntHeaderSize = 0x60;

        /// <summary>
        /// The list of images in the file.
        /// </summary>
        public List<Bitmap> Images { get; set; } = new List<Bitmap>();

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
                        case "MFTG":
                        case "HFPR":
                            var packetHeader = br.ReadStruct<PacketHeader>();
                            _dataBlocks.Add((packetHeader, br.ReadBytes(packetHeader.PacketSize)));
                            break;
                    }
                }

                // Images
                foreach (var block in _imageBlocks)
                {
                    const int bitDepth = 4;
                    switch (block.Header.Magic)
                    {
                        case "MFNT":
                            // Temporary until we get NibbleOrder in Kanvas
                            var bmp = new Bitmap(bitDepth * 2, block.Data.Length / bitDepth);
                            int x = 0, y = 0;

                            foreach (var b in block.Data)
                            {
                                // HighNibbleFirst
                                var l = Helper.ChangeBitDepth(b >> 4, 4, 8);
                                bmp.SetPixel(x, y, Color.FromArgb(255, l, l, l));
                                l = Helper.ChangeBitDepth(b & 0xF, 4, 8);
                                bmp.SetPixel(x + 1, y, Color.FromArgb(255, l, l, l));
                                x += 2;

                                if (x != bmp.Width) continue;
                                x = 0;
                                y++;
                            }

                            Images.Add(bmp);
                            break;
                        case "MFGT":
                            // This is some sort of data
                            break;
                        case "HFPR":
                            // This is some sort of data
                            break;
                    }
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

            }
        }
    }
}
