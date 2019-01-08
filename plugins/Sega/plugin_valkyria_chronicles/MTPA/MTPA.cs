using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Komponent.IO;
using Kontract.Interfaces.Text;

namespace plugin_valkyria_chronicles.MTPA
{
    public sealed class MTPA
    {
        /// <summary>
        /// The size in bytes of the MTPA Header.
        /// </summary>
        private const int MtpaHeaderSize = 0x10;

        /// <summary>
        /// The list of text entries in the file.
        /// </summary>
        public List<TextEntry> Entries { get; set; } = new List<TextEntry>();

        #region InstanceData

        private PacketHeaderX _packetHeader;
        private MTPAHeader _mtpaHeader;
        private List<int> _mtpaHeaderData;
        private List<int> _mtpaTextMetadataPointers;
        private List<ITextMetadata> _mtpaTextMetadata;
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

                // MTPA Header
                _mtpaHeader = br.ReadStruct<MTPAHeader>();
                var metadataSize = _mtpaHeader.MetadataSize;

                // Unknown MTPA Data
                _mtpaHeaderData = br.ReadMultiple<int>(_mtpaHeader.MetadataSize);

                // Text Metadata Pointers
                _mtpaTextMetadataPointers = br.ReadMultiple<int>(_mtpaHeader.PointerCount);

                // Text Metadata
                if (metadataSize == 2)
                    _mtpaTextMetadata = br.ReadMultiple<TextMetadata>(_mtpaHeader.MetadataCount).ToList<ITextMetadata>();
                else if (metadataSize == 4)
                    _mtpaTextMetadata = br.ReadMultiple<TextMetadataX>(_mtpaHeader.MetadataCount).ToList<ITextMetadata>();

                // Text
                var textStart = (int)br.BaseStream.Position;
                var textEnd = _packetHeader.DataSize + _packetHeader.HeaderSize;

                for (var i = 0; i < _mtpaHeader.MetadataCount; i++)
                {
                    var offset = textStart;
                    var unused = false;

                    offset += _mtpaTextMetadata[i].Offset;

                    br.BaseStream.Position = offset;
                    var length = br.ReadROTnInt32();

                    if (length == 0)
                    {
                        var currentOffset = _mtpaTextMetadata[i].Offset;
                        unused = true;

                        if (i != _mtpaHeader.MetadataCount - 1)
                            length = _mtpaTextMetadata[i + 1].Offset - currentOffset - 4;
                        else
                            length = textEnd - currentOffset - 4;
                    }

                    var str = br.ReadROTnBytes(length);

                    Entries.Add(new TextEntry
                    {
                        Name = _mtpaTextMetadata[i].ID.ToString(),
                        EditedText = Encoding.GetEncoding("shift-jis").GetString(str).Trim('\0'),
                        Notes = unused ? "Text length was set to zero. This line might be unused." : string.Empty
                    });
                }

                br.BaseStream.Position = textEnd;

                // ENRS
                _enrs = br.ReadStruct<PacketHeaderX>();
                if (br.BaseStream.Position + _enrs.DataSize + Common.PacketHeaderXSize * 2 <= br.BaseStream.Length)
                {
                    _enrsData = br.ReadBytes(_enrs.DataSize);
                    _enrsFooter = br.ReadStruct<PacketHeaderX>();
                }
                else
                    br.BaseStream.Position = br.BaseStream.Length - Common.PacketHeaderXSize;

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
                var metadataSize = _mtpaHeader.MetadataSize;

                // Move passed the headers.
                bw.BaseStream.Position = Common.PacketHeaderXSize + MtpaHeaderSize + _mtpaHeaderData.Count * sizeof(int);

                // Write the metadata pointers which don't change (add not supported).
                foreach (var pointer in _mtpaTextMetadataPointers)
                    bw.Write(pointer);

                // Move passed the metadata.
                var metadataStart = bw.BaseStream.Position;
                bw.BaseStream.Position += _mtpaHeader.MetadataCount * sizeof(int) * metadataSize;

                // Write out the text strings.
                var textStart = (int)bw.BaseStream.Position;
                for (var i = 0; i < Entries.Count; i++)
                {
                    var text = Encoding.GetEncoding("Shift-JIS").GetBytes(Entries[i].EditedText);
                    _mtpaTextMetadata[i].Offset = (int)bw.BaseStream.Position - textStart;
                    bw.WriteROTnInt32(text.Length);
                    bw.WriteROTnBytes(text);
                    bw.Write((byte)0x01);
                    bw.WriteAlignment(4, 0x01);
                }
                bw.WriteAlignment(8);

                // Update Packet Header
                _packetHeader.DataSize = (int)bw.BaseStream.Position - Common.PacketHeaderXSize;

                // ENRS
                bw.WriteStruct(_enrs);
                bw.Write(_enrsData);
                bw.WriteStruct(_enrsFooter);

                // Update Packet Header
                _packetHeader.PacketSize = (int)bw.BaseStream.Position - Common.PacketHeaderXSize;

                // Footer
                bw.WriteStruct(_mtpaFooter);

                // Write Text Metadata
                bw.BaseStream.Position = metadataStart;
                foreach (var pointer in _mtpaTextMetadata)
                    bw.WriteStruct(pointer);

                // Write Packer Header
                bw.BaseStream.Position = 0;
                bw.WriteStruct(_packetHeader);

                // Write MTPA Header
                bw.WriteStruct(_mtpaHeader);

                // Write Unknown MTPA Data
                foreach (var data in _mtpaHeaderData)
                    bw.Write(data);
            }
        }
    }
}
