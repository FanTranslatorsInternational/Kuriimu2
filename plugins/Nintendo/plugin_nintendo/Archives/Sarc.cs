using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Extensions;
using Kontract.Models.Archive;
using Kontract.Models.IO;
using Kryptography.Hash;

namespace plugin_nintendo.Archives
{
    class Sarc
    {
        private static readonly int HeaderSize = Tools.MeasureType(typeof(SarcHeader));
        private static readonly int SfatHeaderSize = Tools.MeasureType(typeof(SfatHeader));
        private static readonly int SfatEntrySize = Tools.MeasureType(typeof(SfatEntry));
        private static readonly int SfntHeaderSize = Tools.MeasureType(typeof(SfntHeader));

        private ByteOrder _byteOrder;
        private SarcHeader _header;
        private SfatHeader _sfatHeader;
        private SfntHeader _sfntHeader;

        public IList<IArchiveFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true, ByteOrder.BigEndian);

            // Determine byte order
            input.Position = 0x6;
            br.ByteOrder = _byteOrder = br.ReadType<ByteOrder>();

            // Read header
            input.Position = 0;
            _header = br.ReadType<SarcHeader>();

            // Read entries
            _sfatHeader = br.ReadType<SfatHeader>();
            var entries = br.ReadMultiple<SfatEntry>(_sfatHeader.entryCount);

            // Read names
            BinaryReaderX nameBr = null;
            if (entries.Any(x => (x.Flags & 0x100) > 0))
            {
                _sfntHeader = br.ReadType<SfntHeader>();
                var nameStream = new SubStream(input, input.Position, _header.dataOffset - input.Position);
                nameBr = new BinaryReaderX(nameStream);
            }

            // Add files
            var result = new List<IArchiveFileInfo>();
            foreach (var entry in entries)
            {
                var fileStream = new SubStream(input, _header.dataOffset + entry.startOffset, entry.endOffset - entry.startOffset);
                var magic = SarcSupport.DetermineMagic(fileStream);

                var name = $"{entry.nameHash:X8}{SarcSupport.DetermineExtension(magic)}";
                if (nameBr != null)
                {
                    nameBr.BaseStream.Position = entry.FntOffset;
                    name = nameBr.ReadCStringASCII();
                }

                result.Add(new SarcArchiveFileInfo(fileStream, name, magic, entry));
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFileInfo> files, bool isCompressed)
        {
            var simpleHash = new SimpleHash(_sfatHeader.hashMultiplier);
            using var bw = new BinaryWriterX(output, true, _byteOrder);

            var sortedFiles = files.Cast<SarcArchiveFileInfo>().OrderBy(x => _sfntHeader == null ? x.Entry.nameHash : simpleHash.ComputeValue(x.FilePath.ToRelative().FullName)).ToArray();

            // Calculate offsets
            var sfatOffset = HeaderSize;
            var sfntOffset = sfatOffset + SfatHeaderSize + files.Count * SfatEntrySize;
            var dataOffset = _sfntHeader == null
                ? sfntOffset + SfntHeaderSize
                : sfntOffset + SfntHeaderSize + files.Sum(x => (x.FilePath.ToRelative().FullName.Length + 4) & ~3);

            var alignment = sortedFiles.Max(x => SarcSupport.DetermineAlignment(x, _byteOrder, isCompressed));
            var alignedDataOffset = (dataOffset + alignment - 1) & ~(alignment - 1);

            // Write files
            var entries = new List<SfatEntry>();
            var strings = new List<string>();

            var stringPosition = 0;
            var dataPosition = alignedDataOffset;
            foreach (var file in sortedFiles)
            {
                // Write file data
                alignment = SarcSupport.DetermineAlignment(file, _byteOrder, isCompressed);
                var alignedDataPosition = (dataPosition + alignment - 1) & ~(alignment - 1);

                output.Position = alignedDataPosition;
                var writtenSize = file.SaveFileData(output);

                // Add entry
                entries.Add(new SfatEntry
                {
                    startOffset = alignedDataPosition - alignedDataOffset,
                    endOffset = (int)(alignedDataPosition + writtenSize - alignedDataOffset),
                    Flags = (short)(_sfntHeader == null ? 0 : 0x100),
                    FntOffset = (short)(_sfntHeader == null ? 0 : stringPosition),
                    nameHash = _sfntHeader == null ? file.Entry.nameHash : simpleHash.ComputeValue(file.FilePath.ToRelative().FullName)
                });

                // Add string
                strings.Add(file.FilePath.ToRelative().FullName);

                dataPosition = (int)(alignedDataPosition + writtenSize);
                stringPosition += (file.FilePath.ToRelative().FullName.Length + 4) & ~3;
            }

            // Write SFNT
            output.Position = sfntOffset;
            bw.WriteType(new SfntHeader());

            if (_sfntHeader != null)
            {
                foreach (var s in strings)
                {
                    bw.WriteString(s, Encoding.ASCII, false);
                    bw.WriteAlignment(4);
                }
            }

            // Write SFAT
            output.Position = sfatOffset;
            bw.WriteType(new SfatHeader { entryCount = (short)files.Count, hashMultiplier = _sfatHeader.hashMultiplier });
            bw.WriteMultiple(entries);

            // Write header
            output.Position = 0;
            bw.WriteType(new SarcHeader { byteOrder = _byteOrder, dataOffset = alignedDataOffset, fileSize = (int)output.Length, unk1 = _header.unk1 });
        }
    }
}
