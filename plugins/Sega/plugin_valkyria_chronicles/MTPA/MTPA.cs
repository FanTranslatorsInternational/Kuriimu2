using System.Collections.Generic;
using System.IO;
using System.Text;
using Komponent.IO;
using Kontract.Interfaces;

namespace plugin_valkyria_chronicles.MTPA
{
    public sealed class MTPA
    {
        /// <summary>
        /// The size in bytes of the MTPA Packet Header.
        /// </summary>
        private const int MtpaPacketHeaderSize = 0x10;

        /// <summary>
        /// The list of text entries in the file.
        /// </summary>
        public List<TextEntry> Entries { get; set; } = new List<TextEntry>();

        #region InstanceData

        private PacketHeaderX _packetHeader;
        private MTPAPacketHeader _mtpaPacketHeader;
        private List<int> _mtpaPacketHeaderData;
        private List<int> _mtpaTextMetadataPointers;
        private List<TextMetadata> _mtpaTextMetadata;
        private List<TextMetadataX> _mtpaTextMetadataX;
        private PacketHeaderX _enrs;
        private byte[] _enrsData;
        private PacketHeaderX _enrsFooter;
        private PacketHeaderX _mtpaFooter;

        #endregion

        /// <summary>
        /// Read an MTPA file into memory.
        /// </summary>
        /// <param name="input">A readable stream of an MTPA file.</param>
        public MTPA(Stream input)
        {
            using (var br = new BinaryReaderX(input))
            {
                // Packet Header
                _packetHeader = br.ReadStruct<PacketHeaderX>();

                // MTPA Packet Header
                _mtpaPacketHeader = br.ReadStruct<MTPAPacketHeader>();
                var dataSize = _mtpaPacketHeader.DataSize;

                // Unknown Packet Data
                _mtpaPacketHeaderData = br.ReadMultiple<int>(_mtpaPacketHeader.DataSize);

                // Text Metadata Pointers
                _mtpaTextMetadataPointers = br.ReadMultiple<int>(_mtpaPacketHeader.PointerCount);

                // Text Metadata
                if (dataSize == 2)
                    _mtpaTextMetadata = br.ReadMultiple<TextMetadata>(_mtpaPacketHeader.DataCount);
                else if (dataSize == 4)
                    _mtpaTextMetadataX = br.ReadMultiple<TextMetadataX>(_mtpaPacketHeader.DataCount);

                // Text
                var textStart = (int)br.BaseStream.Position;
                var textEnd = _packetHeader.DataSize + _packetHeader.HeaderSize;

                for (var i = 0; i < _mtpaPacketHeader.DataCount; i++)
                {
                    var offset = textStart;
                    var unused = false;

                    if (dataSize == 2)
                        offset += _mtpaTextMetadata[i].Offset;
                    else if (dataSize == 4)
                        offset += _mtpaTextMetadataX[i].Offset;

                    br.BaseStream.Position = offset;
                    var length = br.ReadROTnInt32();

                    if (length == 0)
                    {
                        var currentOffset = (dataSize == 2 ? _mtpaTextMetadata[i].Offset : _mtpaTextMetadataX[i].Offset);
                        unused = true;

                        if (i != _mtpaPacketHeader.DataCount - 1)
                            length = (dataSize == 2 ? _mtpaTextMetadata[i + 1].Offset : _mtpaTextMetadataX[i + 1].Offset) - currentOffset - 4;
                        else
                            length = textEnd - currentOffset - 4;
                    }

                    var str = br.ReadROTnBytes(length);

                    Entries.Add(new TextEntry
                    {
                        Name = dataSize == 2 ? _mtpaTextMetadata[i].ID.ToString() : _mtpaTextMetadataX[i].ID.ToString(),
                        EditedText = Encoding.GetEncoding("shift-jis").GetString(str).Trim('\0'),
                        Notes = unused ? "Text length was set to zero. This line might be unused." : string.Empty
                    });
                }

                br.BaseStream.Position = textEnd;

                // ENRS
                _enrs = br.ReadStruct<PacketHeaderX>();
                if (br.BaseStream.Position + sizeof(int) * 0x8 + _enrs.DataSize + sizeof(int) * 0x10 <= br.BaseStream.Length)
                {
                    _enrsData = br.ReadBytes(_enrs.DataSize);
                    _enrsFooter = br.ReadStruct<PacketHeaderX>();
                }
                else
                    br.BaseStream.Position = br.BaseStream.Length - sizeof(int) * 0x8;

                // MTPA Footer
                _mtpaFooter = br.ReadStruct<PacketHeaderX>();
            }
        }

        /// <summary>
        /// Write an MTPA file to disk.
        /// </summary>
        /// <param name="output">A writable stream of an MTPA file.</param>
        public void Save(Stream output)
        {
            using (var bw = new BinaryWriterX(output))
            {
                var dataSize = _mtpaPacketHeader.DataSize;

                // Move to the beginning of the text data
                bw.BaseStream.Position = Common.PacketHeaderXSize + MtpaPacketHeaderSize + _mtpaPacketHeaderData.Count * sizeof(int) + _mtpaTextMetadataPointers.Count * sizeof(int);
                bw.BaseStream.Position += _mtpaPacketHeader.DataCount * sizeof(int) * dataSize;

                var textStart = (int)bw.BaseStream.Position;


            }
        }
    }
}
