using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        private byte _xorKey = 0x01;

        private List<Table1Metadata> _table1Metadata;
        private List<Table1Object> _table1Entries;
        private List<Table1TextEntry> _textEntries;
        private Dictionary<int, string> _editableTexts;

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

                // Rolling XOR Encryption
                Stream ss;
                if (_mxecPacketHeader.Flags3 == 0x0C)
                {
                    var bytes = br.ReadBytes(_mxecPacketHeader.DataSize);
                    var b = _xorKey = bytes[0];
                    ss = new MemoryStream { Capacity = _mxecPacketHeader.DataSize };

                    using (var bw = new BinaryWriterX(ss, true))
                    {
                        bw.Write((byte)0x01);
                        for (var i = 1; i < bytes.Length; i++)
                        {
                            var bs = bytes[i];
                            bw.Write((byte)(bs ^ b));
                            b = bs;
                        }
                    }

                    ss.Position = 0;
                }
                else
                    ss = new SubStream(input, br.BaseStream.Position, _mxecPacketHeader.DataSize);

                // Table1 Binary Reader
                var bbr = new BinaryReaderX(ss);

                // MXEC Header
                _mxecHeader = bbr.ReadStruct<MXECHeader>();

                // No Table 1
                if (_mxecHeader.Table1Count < 0 || _mxecHeader.Table1Count > 0 && (_mxecHeader.Table2Offset < 0 || _mxecHeader.Table4Offset < 0 || _mxecHeader.Table6Offset < 0))
                    throw new Exception("Table1 doesn't exist in this MXE.");

                // Unsupported Tables
                if (_mxecHeader.Table2Offset > 0)
                    throw new Exception("Table2 is not supported by this plugin.");
                if (_mxecHeader.Table4Offset > 0)
                    throw new Exception("Table4 is not supported by this plugin.");
                if (_mxecHeader.Table6Offset > 0)
                    throw new Exception("Table6 is not supported by this plugin.");

                // Table1 Metadata
                _table1Metadata = bbr.ReadMultiple<Table1Metadata>(_mxecHeader.Table1Count);
                _table1Entries = new List<Table1Object>();

                // Table1
                foreach (var metadata in _table1Metadata)
                {
                    var entry = new Table1Object { Metadata = metadata };
                    _table1Entries.Add(entry);

                    bbr.BaseStream.Position = metadata.TypeOffset - Common.PacketHeaderXSize;
                    entry.Type = bbr.ReadCStringSJIS();

                    bbr.BaseStream.Position = metadata.DataOffset - Common.PacketHeaderXSize;
                    entry.Data = bbr.ReadBytes(metadata.DataSize);
                }

                bbr.SeekAlignment();

                // Text Dimensions
                var textStart = bbr.BaseStream.Position - Common.PacketHeaderXSize;
                var textEnd = _mxecPacketHeader.DataSize - Common.PacketHeaderXSize;

                // Text
                _textEntries = new List<Table1TextEntry>();
                while (true)
                {
                    var entry = new Table1TextEntry { Offset = (int)bbr.BaseStream.Position };
                    var text = bbr.ReadCStringSJIS();
                    if (text == "" && _textEntries.Count > 0)
                        break;
                    entry.Text = text;
                    _textEntries.Add(entry);
                    if (bbr.BaseStream.Position >= textEnd)
                        break;
                }

                bbr.SeekAlignment();

                // Entry Object Interrogator
                _editableTexts = new Dictionary<int, string>();
                foreach (var entry in _table1Entries)
                {
                    entry.TypeIndex = _textEntries.IndexOf(_textEntries.FirstOrDefault(t => t.Offset == entry.Metadata.TypeOffset));

                    var data = entry.Data as byte[];
                    for (var i = 0; i < data.Length / 4; i++)
                    {
                        var val = BitConverter.ToInt32(data, i * 4) - Common.PacketHeaderXSize;

                        if (val >= textStart && val <= textEnd)
                        {
                            entry.Texts.Add(new Table1ObjectText
                            {
                                DataOffset = i,
                                TextIndex = _textEntries.IndexOf(_textEntries.FirstOrDefault(t => t.Offset == val))
                            });
                            if (!_editableTexts.ContainsKey(val))
                                _editableTexts.Add(val, entry.Type);
                            else if (!_editableTexts[val].Contains(entry.Type))
                                _editableTexts[val] += ", " + entry.Type;
                        }
                    }
                }

                // Sew into TextEntries
                foreach (var entry in _textEntries)
                {
                    if (_editableTexts.ContainsKey(entry.Offset))
                        Entries.Add(new TextEntry
                        {
                            Name = entry.Offset.ToString(),
                            EditedText = entry.Text,
                            Notes = _editableTexts[entry.Offset]
                        });
                }

                // Resume from POF0
                br.BaseStream.Position = Common.PacketHeaderXSize * 2 + _mxecPacketHeader.DataSize;

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
