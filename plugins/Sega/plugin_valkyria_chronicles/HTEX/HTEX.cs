using System.IO;
using Komponent.IO;

namespace plugin_valkyria_chronicles.HTEX
{
    public sealed class HTEX
    {
        /// <summary>
        /// A stream of the contained image.
        /// </summary>
        public Stream ImageStream { get; set; }

        #region InstanceData

        private PacketHeaderX _packetHeader;
        private PacketHeaderX _htsfPacketHeader;
        private HtsfHeader _htsfHeader;

        private PacketHeaderX _htsfFooter;
        private PacketHeaderX _htexFooter;

        #endregion

        /// <summary>
        /// Read an HTEX file into memory.
        /// </summary>
        /// <param name="input">A readable stream of an HTEX file.</param>
        public HTEX(Stream input)
        {
            using (var br = new BinaryReaderX(input))
            {
                // Packet Header
                _packetHeader = br.ReadStruct<PacketHeaderX>();

                // HTSF Packet Header
                _htsfPacketHeader = br.ReadStruct<PacketHeaderX>();

                // HTSF Header
                _htsfHeader = br.ReadStruct<HtsfHeader>();

                var bytes = br.ReadBytes(_htsfPacketHeader.DataSize - Common.PacketHeaderXSize);
                ImageStream = new MemoryStream(bytes);

                //if (input is FileStream stream && !File.Exists(Path.ChangeExtension(stream.Name, "gim")))
                //    File.WriteAllBytes(Path.ChangeExtension(stream.Name, "gim"), bytes);

                // Footers
                _htsfFooter = br.ReadStruct<PacketHeaderX>();
                _htexFooter = br.ReadStruct<PacketHeaderX>();
            }
        }

        /// <summary>
        /// Write an HTEX file to disk.
        /// </summary>
        /// <param name="output">A writable stream of an HTEX file.</param>
        public void Save(Stream output)
        {
            using (var bw = new BinaryWriterX(output))
            {
                bw.BaseStream.Position = Common.PacketHeaderXSize * 3;

                ImageStream.Position = 0;
                ImageStream.CopyTo(bw.BaseStream);
                _htsfPacketHeader.DataSize = (int)bw.BaseStream.Position - Common.PacketHeaderXSize * 2;

                // Footers
                _htsfPacketHeader.PacketSize = (int)bw.BaseStream.Position - Common.PacketHeaderXSize * 2;
                bw.WriteStruct(_htsfFooter);

                _packetHeader.PacketSize = (int)bw.BaseStream.Position - Common.PacketHeaderXSize;
                bw.WriteStruct(_htexFooter);

                // Write Packet Header
                bw.BaseStream.Position = 0;
                bw.WriteStruct(_packetHeader);

                // Write HTSF Packet Header
                bw.WriteStruct(_htsfPacketHeader);

                // Write HTSF Header
                bw.WriteStruct(_htsfHeader);
            }
        }
    }
}
