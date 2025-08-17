using Komponent.IO;
using Konnect.Contract.DataClasses.Plugin.File.Text;
using System.Text;
using Komponent.Contract.Enums;
using Kryptography.Checksum.Crc;

namespace plugin_mt_framework.Texts
{
    class Gmdv2
    {
        private const int HeaderSize_ = 0x28;
        private const int LabelEntrySize_ = 0x14;
        private const int MobileLabelEntrySize_ = 0x20;

        private GmdHeader _header;
        private string _name;
        private uint _mobilePadding;
        private int _keyPair;
        private bool _isMobile;

        public List<TextEntry> Load(Stream input)
        {
            using var br = new BinaryReaderX(input);

            // Set endianess
            if (br.PeekString(4) == "\0DMG")
                br.ByteOrder = ByteOrder.BigEndian;

            // Header
            _header = ReadHeader(br);
            _name = br.ReadString(_header.nameSize);
            input.Position++;

            // Check for platform difference
            int fullSize = HeaderSize_ + _header.nameSize + 1 + _header.labelCount * LabelEntrySize_ + (_header.labelCount > 0 ? 0x400 : 0) + _header.labelSize + _header.sectionSize;
            _isMobile = fullSize != br.BaseStream.Length;

            return _isMobile ? ReadMobile(br) : ReadDefault(br);
        }

        private List<TextEntry> ReadMobile(BinaryReaderX reader)
        {
            // Label Entries
            var labelEntries = _header.labelCount > 0 ? ReadMobileLabelEntries(reader, _header.labelCount) : [];
            _mobilePadding = labelEntries.Length > 0 ? labelEntries[0].zeroPadding : 0;

            // Bucketlist
            if (_header.labelCount > 0)
                reader.BaseStream.Position += 0x800;

            // Labels
            long labelDataOffset = reader.BaseStream.Position;
            string[] labels = ReadLabels(reader, labelEntries, labelDataOffset);

            // Text
            reader.BaseStream.Position = HeaderSize_ + _header.nameSize + 1 + _header.labelCount * MobileLabelEntrySize_ + (_header.labelCount > 0 ? 0x800 : 0) + _header.labelSize;

            _keyPair = GmdSupport.DetectKeypair(reader.BaseStream, reader.BaseStream.Position);
            Stream textStream = GmdSupport.GetXorStream(reader.BaseStream, reader.BaseStream.Position, _keyPair);

            // Text deobfuscation
            using var textBr = new BinaryReaderX(textStream);
            var result = new List<TextEntry>();

            for (var i = 0; i < _header.sectionCount; i++)
            {
                long textPos = textBr.BaseStream.Position;

                int tmp = textBr.ReadByte();
                while (tmp != 0)
                    tmp = textBr.ReadByte();

                long textSize = textBr.BaseStream.Position - textPos;
                textBr.BaseStream.Position = textPos;

                // Get Label if existent
                var labelEntry = labelEntries.FirstOrDefault(x => x.sectionId == i);
                int labelIndex = Array.IndexOf(labelEntries, labelEntry);

                result.Add(new TextEntry
                {
                    Name = labelIndex < labels.Length ? labels[labelIndex] : null,
                    TextData = textBr.ReadBytes((int)textSize - 1),
                    Encoding = Encoding.UTF8
                });

                textBr.BaseStream.Position++;
            }

            return result;
        }

        private List<TextEntry> ReadDefault(BinaryReaderX reader)
        {
            // label entries
            var labelEntries = _header.labelCount > 0 ? ReadLabelEntries(reader, _header.labelCount) : [];

            // Bucketlist
            if (_header.labelCount > 0)
                reader.BaseStream.Position += 0x400;

            // Labels
            long labelDataOffset = reader.BaseStream.Position;
            string[] labels = ReadLabels(reader, labelEntries, labelDataOffset);

            // Text
            reader.BaseStream.Position = HeaderSize_ + _header.nameSize + 1 + _header.labelCount * LabelEntrySize_ + (_header.labelCount > 0 ? 0x400 : 0) + _header.labelSize;

            _keyPair = GmdSupport.DetectKeypair(reader.BaseStream, reader.BaseStream.Position);
            Stream textStream = GmdSupport.GetXorStream(reader.BaseStream, reader.BaseStream.Position, _keyPair);

            // Text deobfuscation
            using var textBr = new BinaryReaderX(textStream);
            var result = new List<TextEntry>();

            for (var i = 0; i < _header.sectionCount; i++)
            {
                long textPos = textBr.BaseStream.Position;

                int tmp = textBr.ReadByte();
                while (tmp != 0)
                    tmp = textBr.ReadByte();

                long textSize = textBr.BaseStream.Position - textPos;
                textBr.BaseStream.Position = textPos;

                // Get Label if existent
                var labelEntry = labelEntries.FirstOrDefault(x => x.sectionId == i);
                int labelIndex = Array.IndexOf(labelEntries, labelEntry);

                result.Add(new TextEntry
                {
                    Name = labelIndex < labels.Length ? labels[labelIndex] : null,
                    TextData = textBr.ReadBytes((int)textSize - 1),
                    Encoding = Encoding.UTF8
                });

                textBr.BaseStream.Position++;
            }

            return result;
        }

        public void Save(IList<TextEntry> entries, Stream output)
        {
            if (_isMobile)
                WriteMobile(entries, output);
            else
                WriteDefault(entries, output);
        }

        private void WriteMobile(IList<TextEntry> entries, Stream output)
        {
            var crc32 = Crc32.Crc32B;

            Stream textStream = new MemoryStream();
            Stream cryptStream = GmdSupport.GetXorStream(textStream, _keyPair);

            // Create label entries and buckets
            var labelEntries = new List<Gmdv2MobileLabelEntry>();
            var bucketLookup = new Dictionary<byte, int>();

            var labelOffset = 0;
            var labelCount = 0;

            for (var i = 0; i < entries.Count; i++)
            {
                cryptStream.Write(entries[i].TextData);
                cryptStream.WriteByte(0);

                if (entries[i].Name is null)
                    continue;

                labelEntries.Add(new Gmdv2MobileLabelEntry
                {
                    sectionId = i,
                    hash1 = ~crc32.ComputeValue(entries[i].Name + entries[i].Name),
                    hash2 = ~crc32.ComputeValue(entries[i].Name + entries[i].Name + entries[i].Name),
                    labelOffset = labelOffset,
                    zeroPadding = _mobilePadding
                });
                labelOffset += entries[i].Name!.Length + 1;

                var bucket = (byte)~crc32.ComputeValue(entries[i].Name!);
                if (!bucketLookup.TryAdd(bucket, labelCount))
                {
                    labelEntries[bucketLookup[bucket]].listLink = labelCount;
                    bucketLookup[bucket] = labelCount;
                }

                labelCount++;
            }

            cryptStream.Flush();

            // Create bucketlist
            var buckets = new long[0x100];
            if (labelCount > 0)
            {
                var labelChainIndex = 0;
                foreach (TextEntry entry in entries)
                {
                    if (entry.Name is null)
                        continue;

                    var bucket = (byte)~crc32.ComputeValue(entry.Name!);
                    if (buckets[bucket] is 0)
                        buckets[bucket] = labelChainIndex is 0 ? -1 : labelChainIndex;

                    labelChainIndex++;
                }
            }

            using var bw = new BinaryWriterX(output);

            // Write name
            output.Position = HeaderSize_;
            bw.WriteString(_name);

            // Write label entries
            WriteMobileLabelEntries(labelEntries, bw);

            // Write bucketlist
            if (labelCount > 0)
                WriteLongs(buckets, bw);

            // Write labels
            long labelPos1 = output.Position;
            WriteLabels(entries, bw);

            long labelSize = output.Length - labelPos1;

            // Write text sections
            textStream.Position = 0;
            textStream.CopyTo(output);

            // Write header
            _header.labelCount = labelCount;
            _header.sectionCount = entries.Count;
            _header.labelSize = (int)labelSize;
            _header.sectionSize = (int)textStream.Length;

            output.Position = 0;
            WriteHeader(_header, bw);
        }

        private void WriteDefault(IList<TextEntry> entries, Stream output)
        {
            var crc32 = Crc32.Crc32B;

            Stream textStream = new MemoryStream();
            Stream cryptStream = GmdSupport.GetXorStream(textStream, _keyPair);

            // Create label entries and buckets
            var labelEntries = new List<Gmdv2LabelEntry>();
            var bucketLookup = new Dictionary<byte, int>();

            var labelOffset = 0;
            var labelCount = 0;

            for (var i = 0; i < entries.Count; i++)
            {
                cryptStream.Write(entries[i].TextData);
                cryptStream.WriteByte(0);

                if (entries[i].Name is null)
                    continue;

                labelEntries.Add(new Gmdv2LabelEntry
                {
                    sectionId = i,
                    hash1 = ~crc32.ComputeValue(entries[i].Name + entries[i].Name),
                    hash2 = ~crc32.ComputeValue(entries[i].Name + entries[i].Name + entries[i].Name),
                    labelOffset = labelOffset
                });
                labelOffset += entries[i].Name!.Length + 1;

                var bucket = (byte)~crc32.ComputeValue(entries[i].Name!);
                if (!bucketLookup.TryAdd(bucket, labelCount))
                {
                    labelEntries[bucketLookup[bucket]].listLink = labelCount;
                    bucketLookup[bucket] = labelCount;
                }

                labelCount++;
            }

            cryptStream.Flush();

            // Create bucketlist
            var buckets = new int[0x100];
            if (labelCount > 0)
            {
                var labelChainIndex = 0;
                foreach (TextEntry entry in entries)
                {
                    if (entry.Name is null)
                        continue;

                    var bucket = (byte)~crc32.ComputeValue(entry.Name!);
                    if (buckets[bucket] is 0)
                        buckets[bucket] = labelChainIndex is 0 ? -1 : labelChainIndex;

                    labelChainIndex++;
                }
            }

            using var bw = new BinaryWriterX(output);

            // Write name
            output.Position = HeaderSize_;
            bw.WriteString(_name);

            // Write label entries
            WriteLabelEntries(labelEntries, bw);

            // Write bucketlist
            if (labelCount > 0)
                WriteInts(buckets, bw);

            // Write labels
            long labelPos1 = output.Position;
            WriteLabels(entries, bw);

            long labelSize = output.Length - labelPos1;

            // Write text sections
            textStream.Position = 0;
            textStream.CopyTo(output);

            // Write header
            _header.labelCount = labelCount;
            _header.sectionCount = entries.Count;
            _header.labelSize = (int)labelSize;
            _header.sectionSize = (int)textStream.Length;

            output.Position = 0;
            WriteHeader(_header, bw);
        }

        private GmdHeader ReadHeader(BinaryReaderX reader)
        {
            return new GmdHeader
            {
                magic = reader.ReadString(4),
                version = reader.ReadInt32(),
                language = (GmdLanguage)reader.ReadInt32(),
                unk1 = reader.ReadInt64(),
                labelCount = reader.ReadInt32(),
                sectionCount = reader.ReadInt32(),
                labelSize = reader.ReadInt32(),
                sectionSize = reader.ReadInt32(),
                nameSize = reader.ReadInt32()
            };
        }

        private Gmdv2LabelEntry[] ReadLabelEntries(BinaryReaderX reader, int count)
        {
            var result = new Gmdv2LabelEntry[count];

            for (var i = 0; i < count; i++)
                result[i] = ReadLabelEntry(reader);

            return result;
        }

        private Gmdv2LabelEntry ReadLabelEntry(BinaryReaderX reader)
        {
            return new Gmdv2LabelEntry
            {
                sectionId = reader.ReadInt32(),
                hash1 = reader.ReadUInt32(),
                hash2 = reader.ReadUInt32(),
                labelOffset = reader.ReadInt32(),
                listLink = reader.ReadInt32()
            };
        }

        private Gmdv2MobileLabelEntry[] ReadMobileLabelEntries(BinaryReaderX reader, int count)
        {
            var result = new Gmdv2MobileLabelEntry[count];

            for (var i = 0; i < count; i++)
                result[i] = ReadMobileLabelEntry(reader);

            return result;
        }

        private Gmdv2MobileLabelEntry ReadMobileLabelEntry(BinaryReaderX reader)
        {
            return new Gmdv2MobileLabelEntry
            {
                sectionId = reader.ReadInt32(),
                hash1 = reader.ReadUInt32(),
                hash2 = reader.ReadUInt32(),
                zeroPadding = reader.ReadUInt32(),
                labelOffset = reader.ReadInt64(),
                listLink = reader.ReadInt64()
            };
        }

        private string[] ReadLabels(BinaryReaderX reader, Gmdv2LabelEntry[] labelEntries, long labelDataOffset)
        {
            var result = new string[labelEntries.Length];

            for (var i = 0; i < labelEntries.Length; i++)
            {
                reader.BaseStream.Position = labelDataOffset + labelEntries[i].labelOffset;
                result[i] = reader.ReadNullTerminatedString();
            }

            return result;
        }

        private string[] ReadLabels(BinaryReaderX reader, Gmdv2MobileLabelEntry[] labelEntries, long labelDataOffset)
        {
            var result = new string[labelEntries.Length];

            for (var i = 0; i < labelEntries.Length; i++)
            {
                reader.BaseStream.Position = labelDataOffset + labelEntries[i].labelOffset;
                result[i] = reader.ReadNullTerminatedString();
            }

            return result;
        }

        private void WriteHeader(GmdHeader header, BinaryWriterX writer)
        {
            writer.WriteString(header.magic, writeNullTerminator: false);
            writer.Write(header.version);
            writer.Write((int)header.language);
            writer.Write(header.unk1);
            writer.Write(header.labelCount);
            writer.Write(header.sectionCount);
            writer.Write(header.labelSize);
            writer.Write(header.sectionSize);
            writer.Write(header.nameSize);
        }

        private void WriteMobileLabelEntries(IList<Gmdv2MobileLabelEntry> entries, BinaryWriterX writer)
        {
            foreach (Gmdv2MobileLabelEntry entry in entries)
                WriteMobileLabelEntry(entry, writer);
        }

        private void WriteMobileLabelEntry(Gmdv2MobileLabelEntry entry, BinaryWriterX writer)
        {
            writer.Write(entry.sectionId);
            writer.Write(entry.hash1);
            writer.Write(entry.hash2);
            writer.Write(entry.zeroPadding);
            writer.Write(entry.labelOffset);
            writer.Write(entry.listLink);
        }

        private void WriteLabelEntries(IList<Gmdv2LabelEntry> entries, BinaryWriterX writer)
        {
            foreach (Gmdv2LabelEntry entry in entries)
                WriteLabelEntry(entry, writer);
        }

        private void WriteLabelEntry(Gmdv2LabelEntry entry, BinaryWriterX writer)
        {
            writer.Write(entry.sectionId);
            writer.Write(entry.hash1);
            writer.Write(entry.hash2);
            writer.Write(entry.labelOffset);
            writer.Write(entry.listLink);
        }

        private void WriteLongs(IList<long> entries, BinaryWriterX writer)
        {
            foreach (long entry in entries)
                writer.Write(entry);
        }

        private void WriteInts(IList<int> entries, BinaryWriterX writer)
        {
            foreach (int entry in entries)
                writer.Write(entry);
        }

        private void WriteLabels(IList<TextEntry> entries, BinaryWriterX writer)
        {
            foreach (TextEntry entry in entries)
                writer.WriteString(entry.Name ?? string.Empty);
        }
    }
}
