using System.Text;
using Komponent.Contract.Enums;
using Komponent.IO;
using Konnect.Contract.Plugin.File.Archive;

namespace plugin_nintendo.Archives
{
    class Narc
    {
        private const int NarcHeaderSize_ = 0x10;
        private const int FatHeaderSize_ = 0xC;
        private const int FatEntrySize_ = 0x8;

        private bool _hasNames;

        public List<IArchiveFile> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Determine byte order
            br.BaseStream.Position = 4;
            br.ByteOrder = (ByteOrder)br.ReadUInt16();

            // Read header
            br.BaseStream.Position = 0;
            var header = ReadHeader(br);

            // Read file entries
            var fatHeader = ReadFatHeader(br);
            var entries = ReadFatEntries(br, fatHeader.fileCount);

            // Read FNT
            var fntOffset = (int)br.BaseStream.Position;
            var fntHeader = ReadFntHeader(br);

            var gmifOffset = fntOffset + fntHeader.chunkSize;

            _hasNames = br.ReadInt32() >= 8;
            if (_hasNames)
                return NdsSupport.ReadFnt(br, fntOffset + 8, gmifOffset + 8, entries).ToList();

            return entries.Select((x, i) => NdsSupport.CreateAfi(br.BaseStream, x.offset + gmifOffset + 8, x.Length, $"{i:00000000}.bin", i)).ToList();
        }

        public void Save(Stream output, List<IArchiveFile> files)
        {
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var fatOffset = NarcHeaderSize_;
            var fntOffset = fatOffset + FatHeaderSize_ + files.Count * FatEntrySize_;

            // Write FNT
            int fntSize;
            if (!_hasNames)
            {
                output.Position = fntOffset + 8;
                bw.Write(4);
                bw.Write(0x10000);
                fntSize = 0x10;
            }
            else
            {
                NdsSupport.WriteFnt(bw, fntOffset + 8, files);
                fntSize = (int)(bw.BaseStream.Position - fntOffset);
            }

            var fntHeader = new NarcFntHeader
            {
                magic = "BTNF",
                chunkSize = fntSize
            };

            output.Position = fntOffset;
            WriteFntHeader(fntHeader, bw);

            // Write GMIF
            var fatEntries = new List<FatEntry>();

            var gmifOffset = fntOffset + fntSize;
            output.Position = gmifOffset + 8;
            foreach (var file in files.Cast<FileIdArchiveFile>().OrderBy(x => x.FileId))
            {
                var filePosition = output.Position;
                var writtenSize = file.WriteFileData(output, true);

                fatEntries.Add(new FatEntry
                {
                    offset = (int)filePosition - gmifOffset - 8,
                    endOffset = (int)(filePosition - gmifOffset - 8 + writtenSize)
                });
            }

            output.Position = gmifOffset;
            bw.WriteString("GMIF", Encoding.ASCII, false, false);
            bw.Write((int)(output.Length - gmifOffset));

            // Write FAT
            var fatHeader = new NarcFatHeader
            {
                magic = "BTAF",
                chunkSize = FatHeaderSize_ + files.Count * FatEntrySize_,
                fileCount = (short)files.Count
            };

            output.Position = fatOffset;
            WriteFatHeader(fatHeader, bw);
            WriteFatEntries(fatEntries, bw);

            // Write header
            var header = new NarcHeader
            {
                magic = "NARC",
                bom = 0xFFFE,
                version = 0x100,
                fileSize = (int)output.Length,
                chunkSize = 0x10,
                chunkCount = 0x3
            };

            output.Position = 0;
            WriteHeader(header, bw);
        }

        private NarcHeader ReadHeader(BinaryReaderX reader)
        {
            return new NarcHeader
            {
                magic = reader.ReadString(4),
                bom = reader.ReadUInt16(),
                version = reader.ReadInt16(),
                fileSize = reader.ReadInt32(),
                chunkSize = reader.ReadInt16(),
                chunkCount = reader.ReadInt16()
            };
        }

        private NarcFatHeader ReadFatHeader(BinaryReaderX reader)
        {
            return new NarcFatHeader
            {
                magic = reader.ReadString(4),
                chunkSize = reader.ReadInt32(),
                fileCount = reader.ReadInt16(),
                reserved1 = reader.ReadInt16()
            };
        }

        private FatEntry[] ReadFatEntries(BinaryReaderX reader, int count)
        {
            var result = new FatEntry[count];

            for (var i = 0; i < count; i++)
                result[i] = ReadFatEntry(reader);

            return result;
        }

        private FatEntry ReadFatEntry(BinaryReaderX reader)
        {
            return new FatEntry
            {
                offset = reader.ReadInt32(),
                endOffset = reader.ReadInt32()
            };
        }

        private NarcFntHeader ReadFntHeader(BinaryReaderX reader)
        {
            return new NarcFntHeader
            {
                magic = reader.ReadString(4),
                chunkSize = reader.ReadInt32()
            };
        }

        private void WriteHeader(NarcHeader header, BinaryWriterX writer)
        {
            writer.WriteString(header.magic, writeNullTerminator: false);
            writer.Write(header.bom);
            writer.Write(header.version);
            writer.Write(header.fileSize);
            writer.Write(header.chunkSize);
            writer.Write(header.chunkCount);
        }

        private void WriteFatHeader(NarcFatHeader header, BinaryWriterX writer)
        {
            writer.WriteString(header.magic, writeNullTerminator: false);
            writer.Write(header.chunkSize);
            writer.Write(header.fileCount);
            writer.Write(header.reserved1);
        }

        private void WriteFatEntries(IList<FatEntry> entries, BinaryWriterX writer)
        {
            foreach (FatEntry entry in entries)
                WriteFatEntry(entry, writer);
        }

        private void WriteFatEntry(FatEntry entry, BinaryWriterX writer)
        {
            writer.Write(entry.offset);
            writer.Write(entry.endOffset);
        }

        private void WriteFntHeader(NarcFntHeader header, BinaryWriterX writer)
        {
            writer.WriteString(header.magic, writeNullTerminator: false);
            writer.Write(header.chunkSize);
        }
    }
}
