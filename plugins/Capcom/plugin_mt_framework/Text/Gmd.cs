using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Models.IO;
using Kontract.Models.Text;
using Kryptography.Hash.Crc;
using Microsoft.Toolkit.HighPerformance.Extensions;

namespace plugin_mt_framework.Text
{
    class Gmd
    {
        private static readonly int HeaderSize = Tools.MeasureType(typeof(GmdHeader));
        private static readonly int EntrySizeV1 = Tools.MeasureType(typeof(GmdEntryV1));
        private static readonly int EntrySizeV2 = Tools.MeasureType(typeof(GmdEntryV2));
        private static readonly int EntrySizeV2Mobile = Tools.MeasureType(typeof(GmdEntryV2Mobile));

        private ByteOrder _byteOrder;
        private GmdHeader _header;
        private string _name;

        private int _keyIndex = -1;

        private bool _isMobile;
        private uint _v1SmallestLabelOffset;

        public IList<TextEntry> Load(Stream input)
        {
            using var br = new BinaryReaderX(input);

            // Determine byte order
            _byteOrder = br.ByteOrder = br.ReadString(4) == "GMD\0" ? ByteOrder.LittleEndian : ByteOrder.BigEndian;
            br.BaseStream.Position = 0;

            // Read header
            _header = br.ReadType<GmdHeader>();
            _name = br.ReadString(_header.nameSize, Encoding.ASCII);

            // Skip null byte of name
            input.Position++;

            // Determine reading method
            switch (_header.version)
            {
                // Version 1
                case 0x00010201:
                    return ReadVersion1(br);

                // Version 2
                case 0x00010302:
                    return ReadVersion2(br);

                default:
                    throw new InvalidOperationException($"Unsupported version {_header.version:X8}.");
            }
        }

        public void Save(Stream output, IList<TextEntry> entries)
        {
            using var bw = new BinaryWriterX(output, _byteOrder);

            switch (_header.version)
            {
                // Version 1
                case 0x00010201:
                    SaveVersion1(bw, entries);
                    break;

                // Version 2
                case 0x00010302:
                    SaveVersion2(bw, entries);
                    break;

                default:
                    throw new InvalidOperationException($"Unsupported version {_header.version:X8}.");
            }
        }

        #region Load

        private IList<TextEntry> ReadVersion1(BinaryReaderX br)
        {
            // Read entry infos
            var entries = br.ReadMultiple<GmdEntryV1>(_header.sectionCount);

            // Determine first label offset
            _v1SmallestLabelOffset = (uint)entries.Min(x => x.labelOffset);

            // Skip labels
            var labelOffset = br.BaseStream.Position;
            br.BaseStream.Position += _header.labelSize;

            // Wrap section data into decryption, if necessary
            Stream sectionStream = new SubStream(br.BaseStream, br.BaseStream.Position, _header.sectionSize);
            if (GmdSupport.IsEncrypted(sectionStream))
            {
                _keyIndex = GmdSupport.DetermineKeyIndex(sectionStream);
                sectionStream = GmdSupport.WrapXor(sectionStream, _keyIndex);
            }

            // Read section data
            using var sectionBr = new BinaryReaderX(sectionStream);

            var result = new List<TextEntry>();
            for (var i = 0; i < _header.labelCount; i++)
            {
                var sectionData = sectionBr.ReadBytesUntil(0);
                var label = string.Empty;

                // Get label
                // HINT: Since the first entry can be (0,0) this code can't work with a single FirstOrDefault
                var entryFound = entries.Any(x => x.sectionId == i);
                if (entryFound)
                {
                    var entryInfo = entries.FirstOrDefault(x => x.sectionId == i);

                    var bkPos = br.BaseStream.Position;
                    br.BaseStream.Position = labelOffset + entryInfo.labelOffset - _v1SmallestLabelOffset;
                    label = br.ReadCStringASCII();
                    br.BaseStream.Position = bkPos;
                }

                // Create entry
                var entry = new TextEntry(sectionData, Encoding.UTF8);
                if (!string.IsNullOrEmpty(label))
                    entry.Name = label;
                entry.ControlCodeProcessor = new GmdControlCodeProcessor();
                entry.TextPager = new GmdTextPager();

                result.Add(entry);
            }

            return result;
        }

        private IList<TextEntry> ReadVersion2(BinaryReaderX br)
        {
            // Determine mobile textEntries
            var mobileLength = HeaderSize + _header.labelSize + _header.sectionSize +
                               0x800 + _header.nameSize + 1 + _header.labelCount * EntrySizeV2Mobile;
            _isMobile = br.BaseStream.Length == mobileLength;

            // Read entry info
            (int, long)[] entryInfos;
            if (_isMobile)
            {
                entryInfos = br.ReadMultiple<GmdEntryV2Mobile>(_header.sectionCount)
                    .Select(e => (e.sectionId, e.labelOffset))
                    .ToArray();

                // Skip bucket list
                br.BaseStream.Position += 0x800;
            }
            else
            {
                entryInfos = br.ReadMultiple<GmdEntryV2>(_header.sectionCount)
                    .Select(e => (e.sectionId, (long)e.labelOffset))
                    .ToArray();

                // Skip bucket list
                br.BaseStream.Position += 0x400;
            }

            // Skip labels
            var labelOffset = br.BaseStream.Position;
            br.BaseStream.Position += _header.labelSize;

            // Wrap section data into decryption, if necessary
            Stream sectionStream = new SubStream(br.BaseStream, br.BaseStream.Position, _header.sectionSize);
            if (GmdSupport.IsEncrypted(sectionStream))
            {
                _keyIndex = GmdSupport.DetermineKeyIndex(sectionStream);
                sectionStream = GmdSupport.WrapXor(sectionStream, _keyIndex);
            }

            // Read section data
            using var sectionBr = new BinaryReaderX(sectionStream);

            var result = new List<TextEntry>();
            for (var i = 0; i < _header.labelCount; i++)
            {
                var sectionData = sectionBr.ReadBytesUntil(0);
                var label = string.Empty;

                // Get label
                // HINT: Since the first entry can be (0,0) this code can't work with a single FirstOrDefault
                var entryFound = entryInfos.Any(x => x.Item1 == i);
                if (entryFound)
                {
                    var entryInfo = entryInfos.FirstOrDefault(x => x.Item1 == i);

                    var bkPos = br.BaseStream.Position;
                    br.BaseStream.Position = labelOffset + entryInfo.Item2;
                    label = br.ReadCStringASCII();
                    br.BaseStream.Position = bkPos;
                }

                // Create entry
                var entry = new TextEntry(sectionData, Encoding.UTF8);
                if (!string.IsNullOrEmpty(label))
                    entry.Name = label;
                entry.ControlCodeProcessor = new GmdControlCodeProcessor();
                entry.TextPager = new GmdTextPager();

                result.Add(entry);
            }

            return result;
        }

        #endregion

        #region Save

        private void SaveVersion1(BinaryWriterX bw, IList<TextEntry> textEntries)
        {
            // Calculate offsets
            var nameOffset = HeaderSize;
            var entryOffset = nameOffset + _name.Length + 1;
            var labelOffset = entryOffset + textEntries.Count * EntrySizeV1;
            var sectionOffset = labelOffset + textEntries.Sum(x => x.Name.Length + 1);

            // Write section data
            var sectionSize = textEntries.Sum(x => x.TextData.Length + 1);
            bw.BaseStream.SetLength(sectionOffset + sectionSize);

            Stream sectionStream = new SubStream(bw.BaseStream, sectionOffset, sectionSize);
            if (_keyIndex >= 0)
                sectionStream = GmdSupport.WrapXor(sectionStream, _keyIndex);

            using var sectionBw = new BinaryWriterX(sectionStream);
            foreach (var entry in textEntries)
            {
                sectionBw.Write(entry.TextData);
                sectionBw.Write((byte)0);
            }

            // Write labels
            var entries = new List<GmdEntryV1>();

            bw.BaseStream.Position = labelOffset;
            for (var i = 0; i < textEntries.Count; i++)
            {
                entries.Add(new GmdEntryV1
                {
                    sectionId = i,
                    labelOffset = (uint)(bw.BaseStream.Position - labelOffset + _v1SmallestLabelOffset)
                });

                bw.WriteString(textEntries[i].Name, Encoding.ASCII, false);
            }

            // Write entries
            bw.BaseStream.Position = entryOffset;
            bw.WriteMultiple(entries);

            // Update header
            _header.labelCount = textEntries.Count;
            _header.sectionCount = textEntries.Count;
            _header.labelSize = sectionOffset - labelOffset;
            _header.nameSize = _name.Length;
            _header.sectionSize = (int)bw.BaseStream.Length - sectionOffset;

            // Write header
            bw.BaseStream.Position = 0;
            bw.WriteType(_header);
            bw.WriteString(_name, Encoding.ASCII, false);
        }

        private void SaveVersion2(BinaryWriterX bw, IList<TextEntry> textEntries)
        {
            var crc32 = Crc32.Default;

            // Calculate offsets
            var nameOffset = HeaderSize;
            var entryOffset = nameOffset + _name.Length + 1;
            var bucketOffset = entryOffset + textEntries.Count * (_isMobile ? EntrySizeV2Mobile : EntrySizeV2);
            var labelOffset = bucketOffset + (_isMobile ? 0x800 : 0x400);
            var sectionOffset = labelOffset + textEntries.Sum(x => x.Name.Length + 1);

            // Write section data
            var sectionSize = textEntries.Sum(x => x.TextData.Length + 1);
            bw.BaseStream.SetLength(sectionOffset + sectionSize);

            Stream sectionStream = new SubStream(bw.BaseStream, sectionOffset, sectionSize);
            if (_keyIndex >= 0)
                sectionStream = GmdSupport.WrapXor(sectionStream, _keyIndex);

            using var sectionBw = new BinaryWriterX(sectionStream);
            foreach (var entry in textEntries)
            {
                sectionBw.Write(entry.TextData);
                sectionBw.Write((byte)0);
            }

            // Write labels
            var buckets = new Dictionary<byte, int>();
            var entries = new List<dynamic>();

            bw.BaseStream.Position = labelOffset;
            for (var i = 0; i < textEntries.Count; i++)
            {
                // Add GMD entry
                if (_isMobile)
                {
                    entries.Add(new GmdEntryV2Mobile
                    {
                        sectionId = i,
                        hash1 = ~crc32.ComputeValue(textEntries[i].Name + textEntries[i].Name),
                        hash2 = ~crc32.ComputeValue(textEntries[i].Name + textEntries[i].Name + textEntries[i].Name),
                        labelOffset = bw.BaseStream.Position - labelOffset
                    });
                }
                else
                {
                    entries.Add(new GmdEntryV2
                    {
                        sectionId = i,
                        hash1 = ~crc32.ComputeValue(textEntries[i].Name + textEntries[i].Name),
                        hash2 = ~crc32.ComputeValue(textEntries[i].Name + textEntries[i].Name + textEntries[i].Name),
                        labelOffset = (uint)(bw.BaseStream.Position - labelOffset)
                    });
                }

                // Set list link
                var bucket = (byte)~crc32.ComputeValue(textEntries[i].Name);
                if (buckets.ContainsKey(bucket))
                    entries[buckets[bucket]].listLink = i;
                buckets[bucket] = i;

                // Write label
                bw.WriteString(textEntries[i].Name, Encoding.ASCII, false);
            }

            // Write hash buckets
            var bucketList = new long[0x100];
            for (var i = 0; i < textEntries.Count; i++)
            {
                var bucket = (byte)~crc32.ComputeValue(textEntries[i].Name);
                bucketList[bucket] = i == 0 ? -1 : i;
            }

            bw.BaseStream.Position = bucketOffset;
            if (_isMobile)
                bw.WriteMultiple(bucketList);
            else
                bw.WriteMultiple(bucketList.Select(x => (int)x));

            // Write entries
            bw.BaseStream.Position = entryOffset;
            bw.WriteMultiple(entries);

            // Update header
            _header.labelCount = textEntries.Count;
            _header.sectionCount = textEntries.Count;
            _header.labelSize = sectionOffset - labelOffset;
            _header.nameSize = _name.Length;
            _header.sectionSize = (int)bw.BaseStream.Length - sectionOffset;

            // Write header
            bw.BaseStream.Position = 0;
            bw.WriteType(_header);
            bw.WriteString(_name, Encoding.ASCII, false);
        }

        #endregion
    }
}
