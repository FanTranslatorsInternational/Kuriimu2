using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Komponent.IO;
using Kontract.Interfaces.Text;

namespace plugin_valkyria_chronicles.MXEN
{
    public sealed class MXEN
    {
        /// <summary>
        /// The size in bytes of the MXEC Header.
        /// </summary>
        private const int MxecHeaderSize = 0x80;

        /// <summary>
        /// The list of text entries in the file.
        /// </summary>
        public List<TextEntry> Entries { get; set; } = new List<TextEntry>();

        #region InstanceData

        private PacketHeaderX _packetHeader;
        private PacketHeaderX _mxecPacketHeader;
        private MXECHeader _mxecHeader;
        private byte _xorKey = 0x00;

        private List<Table1Metadata> _table1Metadata;
        private List<Table1Object> _table1Objects;
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
                _packetHeader = br.ReadType<PacketHeaderX>();

                // MXEC Packet Header
                _mxecPacketHeader = br.ReadType<PacketHeaderX>();

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
                _mxecHeader = bbr.ReadType<MXECHeader>();

                // Unsupported Tables
                if (_mxecHeader.Table2Offset > 0)
                    throw new Exception("Table2 is not supported by this plugin.");
                if (_mxecHeader.Table4Offset > 0)
                    throw new Exception("Table4 is not supported by this plugin.");
                if (_mxecHeader.Table6Offset > 0)
                    throw new Exception("Table6 is not supported by this plugin.");

                // Table1 Metadata
                _table1Metadata = bbr.ReadMultiple<Table1Metadata>(_mxecHeader.Table1Count);
                _table1Objects = new List<Table1Object>();

                // Table1
                foreach (var metadata in _table1Metadata)
                {
                    var obj = new Table1Object { Metadata = metadata };
                    _table1Objects.Add(obj);

                    bbr.BaseStream.Position = metadata.TypeOffset - Common.PacketHeaderXSize;
                    obj.Type = bbr.ReadCStringSJIS();

                    bbr.BaseStream.Position = metadata.DataOffset - Common.PacketHeaderXSize;
                    obj.Data = bbr.ReadBytes(metadata.DataSize);
                }

                bbr.SeekAlignment();

                // Text
                var textStart = bbr.BaseStream.Position;
                var textEnd = _mxecPacketHeader.DataSize;

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

                _editableTexts = new Dictionary<int, string>();
                foreach (var entry in _table1Objects)
                {
                    entry.TypeIndex = _textEntries.IndexOf(_textEntries.FirstOrDefault(t => t.Offset == entry.Metadata.TypeOffset - Common.PacketHeaderXSize));

                    for (var i = 0; i < entry.Data.Length / 4; i++)
                    {
                        var val = BitConverter.ToInt32(entry.Data, i * 4) - Common.PacketHeaderXSize;

                        if (val >= textStart && val <= textEnd)
                        {
                            var index = _textEntries.IndexOf(_textEntries.FirstOrDefault(t => t.Offset == val));
                            if (index <= -1) continue;

                            entry.Texts.Add(new Table1ObjectText
                            {
                                DataOffset = i * 4,
                                TextIndex = index
                            });

                            if (!_editableTexts.ContainsKey(val))
                                _editableTexts.Add(val, entry.Type);
                            else if (!_editableTexts[val].Contains(entry.Type))
                                _editableTexts[val] += ", " + entry.Type;
                        }
                    }
                }

                var id = 1;
                foreach (var entry in _textEntries)
                {
                    if (!_editableTexts.ContainsKey(entry.Offset)) continue;
                    entry.ID = id;
                    Entries.Add(new TextEntry
                    {
                        Name = entry.ID.ToString(),
                        EditedText = entry.Text,
                        Notes = _editableTexts[entry.Offset]
                    });
                    id++;
                }

                // Reset Stream
                br.BaseStream.Position = Common.PacketHeaderXSize * 2 + _mxecPacketHeader.DataSize;

                // POF0
                _pof0 = br.ReadType<PacketHeaderX>();
                _pof0Data = br.ReadBytes(_pof0.DataSize);

                // ENRS
                _enrs = br.ReadType<PacketHeaderX>();
                _enrsData = br.ReadBytes(_enrs.DataSize);

                // CCRS
                _ccrs = br.ReadType<PacketHeaderX>();
                _ccrsData = br.ReadBytes(_ccrs.DataSize);

                // Footers
                _ccrsFooter = br.ReadType<PacketHeaderX>();
                _mxecFooter = br.ReadType<PacketHeaderX>();
                _mxenFooter = br.ReadType<PacketHeaderX>();
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
                // Table1 MemoryStream
                var ms = new MemoryStream();

                // Write Table1
                using (var bbw = new BinaryWriterX(ms, true))
                {
                    // Skip MXEC Header
                    ms.Position += MxecHeaderSize;

                    // Skip Table1Metadata
                    ms.Position += sizeof(int) * 4 * _table1Metadata.Count;

                    // Skip Table1Objects
                    foreach (var obj in _table1Objects)
                    {
                        if (obj.Metadata.DataOffset + obj.Data.Length > ms.Position)
                            ms.Position = obj.Metadata.DataOffset + obj.Data.Length - Common.PacketHeaderXSize;
                    }

                    bbw.WriteAlignment();

                    // Write Texts
                    foreach (var entry in _textEntries)
                    {
                        entry.Text = Entries.FirstOrDefault(e => e.Name == entry.ID.ToString())?.EditedText ?? entry.Text;
                        entry.Offset = (int)bbw.BaseStream.Position;
                        bbw.Write(Encoding.GetEncoding("Shift-JIS").GetBytes(entry.Text));
                        bbw.Write((byte)0x0);
                    }

                    bbw.WriteAlignment();

                    // Update Table1 Size
                    _mxecPacketHeader.DataSize = (int)ms.Position;

                    // Write MXEC Header
                    ms.Position = 0;
                    bbw.WriteType(_mxecHeader);

                    // Update All Text Pointers
                    foreach (var entry in _table1Objects)
                    {
                        entry.Metadata.TypeOffset = _textEntries[entry.TypeIndex].Offset + Common.PacketHeaderXSize;

                        foreach (var text in entry.Texts)
                        {
                            var val = BitConverter.GetBytes(_textEntries[text.TextIndex].Offset + Common.PacketHeaderXSize);
                            for (var i = 0; i < val.Length; i++)
                                entry.Data[text.DataOffset + i] = val[i];
                        }
                    }

                    // Write Table1Metadata
                    bbw.WriteMultiple(_table1Metadata);

                    // Write Table1Objects
                    foreach (var obj in _table1Objects)
                    {
                        bbw.Write(obj.Data);
                        bbw.WriteAlignment();
                    }

                    bbw.WriteAlignment();

                    // Write out to the base stream.
                    bw.BaseStream.Position = Common.PacketHeaderXSize * 2;
                    ms.Position = 0;
                    if (_xorKey != 0x0)
                    {
                        var b = _xorKey;
                        using (var br = new BinaryReaderX(ms, true))
                        {
                            bw.Write(_xorKey);
                            br.BaseStream.Position = 1;
                            for (var i = 1; i < br.BaseStream.Length; i++)
                            {
                                var bs = br.ReadByte();
                                bw.Write((byte)(bs ^ b));
                                b = (byte)(bs ^ b);
                            }
                        }
                    }
                    else
                        ms.CopyTo(bw.BaseStream);
                }

                // Reset Stream
                bw.BaseStream.Position = Common.PacketHeaderXSize * 2 + _mxecPacketHeader.DataSize;

                // POF0
                bw.WriteType(_pof0);
                bw.Write(_pof0Data);

                // ENRS
                bw.WriteType(_enrs);
                bw.Write(_enrsData);

                // CCRS
                bw.WriteType(_ccrs);
                bw.Write(_ccrsData);

                // Footers
                bw.WriteType(_ccrsFooter);

                _mxecPacketHeader.PacketSize = (int)bw.BaseStream.Position - Common.PacketHeaderXSize * 2;
                bw.WriteType(_mxecFooter);

                _packetHeader.PacketSize = (int)bw.BaseStream.Position - Common.PacketHeaderXSize;
                bw.WriteType(_mxenFooter);

                // Write Packet Header
                bw.BaseStream.Position = 0;
                bw.WriteType(_packetHeader);

                // Write MXEC Packet Header
                bw.WriteType(_mxecPacketHeader);
            }
        }
    }
}
