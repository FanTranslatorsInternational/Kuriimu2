using System;
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
        /// The list of text entries in the file.
        /// </summary>
        public List<TextEntry> Entries { get; set; }

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
        /// <param name="input">A readable stream to an MTPA file.</param>
        public MTPA(Stream input)
        {
            Entries = new List<TextEntry>();

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
                    var length = ReadROT1Int32(br);

                    if (length == 0)
                    {
                        var currentOffset = (dataSize == 2 ? _mtpaTextMetadata[i].Offset : _mtpaTextMetadataX[i].Offset);
                        unused = true;

                        if (i != _mtpaPacketHeader.DataCount - 1)
                            length = (dataSize == 2 ? _mtpaTextMetadata[i + 1].Offset : _mtpaTextMetadataX[i + 1].Offset) - currentOffset - 4;
                        else
                            length = textEnd - currentOffset - 4;
                    }

                    var str = br.ReadBytes(Math.Max(length, 0));
                    for (var j = 0; j < str.Length; j++)
                        str[j] -= 1;

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

        private int ReadROT1Int32(BinaryReader br)
        {
            var oi = br.ReadBytes(4);
            if (oi[0] > 0) oi[0] -= 1;
            if (oi[1] > 0) oi[1] -= 1;
            if (oi[2] > 0) oi[2] -= 1;
            if (oi[3] > 0) oi[3] -= 1;
            return BitConverter.ToInt32(oi, 0);
        }
    }
}
