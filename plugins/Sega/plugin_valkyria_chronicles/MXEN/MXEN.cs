using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Komponent.IO;
using Kontract.Interfaces;

namespace plugin_valkyria_chronicles.MXEN
{
    public sealed class MXEN
    {
        /// <summary>
        /// The size in bytes of the MXEN Header.
        /// </summary>
        private const int MtpaHeaderSize = 0x10;

        /// <summary>
        /// The list of text entries in the file.
        /// </summary>
        public List<TextEntry> Entries { get; set; } = new List<TextEntry>();

        #region InstanceData

        private PacketHeaderX _packetHeader;
        private PacketHeaderX _mxecPacketHeader;
        private MXECHeader _mxecHeader;

        private List<Table1Metadata> _table1Metadata;
        private List<Table1Entry> _table1Entries;
        private List<MxecTextEntry> _textEntries;

        private PacketHeaderX _pof0;
        private byte[] _pof0Data;

        private PacketHeaderX _enrs;
        private byte[] _enrsData;

        private PacketHeaderX _ccrs;
        private byte[] _ccrsData;

        private PacketHeaderX _ccrsFooter;
        private PacketHeaderX _mxecFooter;
        private PacketHeaderX _mxenFooter;

        #endregion

        /// <summary>
        /// Read an MXEN file into memory.
        /// </summary>
        /// <param name="input">A readable stream of an MXEN file.</param>
        public MXEN(Stream input)
        {
            using (var br = new BinaryReaderX(input))
            {
                // Packet Header
                _packetHeader = br.ReadStruct<PacketHeaderX>();

                // MXEC Packet Header
                _mxecPacketHeader = br.ReadStruct<PacketHeaderX>();

                // MXEC Header
                _mxecHeader = br.ReadStruct<MXECHeader>();

                // Table1 Metadata
                _table1Metadata = br.ReadMultiple<Table1Metadata>(_mxecHeader.Table1Count);
                _table1Entries = new List<Table1Entry>();

                // Table1
                foreach (var metadata in _table1Metadata)
                {
                    var entry = new Table1Entry { Metadata = metadata };

                    br.BaseStream.Position = Common.PacketHeaderXSize + metadata.TypeOffset;
                    entry.Type = br.ReadCStringSJIS();

                    br.BaseStream.Position = Common.PacketHeaderXSize + metadata.DataOffset;
                    switch (entry.Type)
                    {
                        case "VlMxSlgLandformInfo":
                            entry.Data = br.ReadStruct<VlMxSlgLandformInfo>();
                            break;
                        default:
                            entry.Data = br.ReadBytes(metadata.DataSize);
                            break;
                    }

                    _table1Entries.Add(entry);
                }

                br.SeekAlignment();

                // Text
                _textEntries = new List<MxecTextEntry>();
                while (true)
                {
                    var entry = new MxecTextEntry { Offset = (int)br.BaseStream.Position - Common.PacketHeaderXSize };
                    var text = br.ReadCStringSJIS();
                    if (text == string.Empty)
                        break;
                    entry.Text = text;
                    _textEntries.Add(entry);
                }

                br.SeekAlignment();

                // Sew into TextEntries
                foreach (var entry in _table1Entries)
                {
                    switch (entry.Type)
                    {
                        case "VlMxSlgLandformInfo":
                            var data = entry.Data as VlMxSlgLandformInfo;
                            entry.TextIndex = _textEntries.IndexOf(_textEntries.FirstOrDefault(t => t.Offset == data.TypeOffset));
                            break;
                        default:
                            continue;
                    }

                    Entries.Add(new TextEntry
                    {
                        Name = entry.Metadata.ID.ToString(),
                        EditedText = _textEntries[entry.TextIndex].Text
                    });
                }

                // POF0
                _pof0 = br.ReadStruct<PacketHeaderX>();
                _pof0Data = br.ReadBytes(_pof0.DataSize);

                // ENRS
                _enrs = br.ReadStruct<PacketHeaderX>();
                _enrsData = br.ReadBytes(_enrs.DataSize);

                // CCRS
                _ccrs = br.ReadStruct<PacketHeaderX>();
                _ccrsData = br.ReadBytes(_ccrs.DataSize);

                // Footers
                _ccrsFooter = br.ReadStruct<PacketHeaderX>();
                _mxecFooter = br.ReadStruct<PacketHeaderX>();
                _mxenFooter = br.ReadStruct<PacketHeaderX>();
            }
        }

        /// <summary>
        /// Write an MXEN file to disk.
        /// </summary>
        /// <param name="output">A writable stream of an MXEN file.</param>
        public void Save(Stream output)
        {
            using (var bw = new BinaryWriterX(output))
            {

            }
        }
    }
}
