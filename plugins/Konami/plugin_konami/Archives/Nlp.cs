using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Archive;

namespace plugin_konami.Archives
{
    class Nlp
    {
        private const int BlockSize = 0x800;

        private static readonly int MetaSize = 0x14;
        private static readonly int BlockOffsetHeaderSize = 0xC;
        private static readonly int BlockOffsetSize = 0x8;

        private NlpHeader _header;

        public List<IArchiveFile> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            _header = ReadHeader(br);

            // Read meta
            input.Position = BlockSize;
            var metas = ReadMetas(br, _header.entryCount);

            // Read block offsets
            input.Position = _header.blockEntriesOffset + BlockSize;
            var blockOffsetHeader = ReadOffsetHeader(br);
            var blockOffsets = ReadOffsets(br, blockOffsetHeader.entryCount);

            // Add files
            var result = new List<IArchiveFile>();
            for (var i = 0; i < blockOffsetHeader.entryCount; i++)
            {
                var blockOffset = blockOffsets[i];
                var meta = metas[blockOffset.metaId];

                var offset = blockOffset.offset * BlockSize + BlockSize;
                var calculatedSize = (i + 1 == blockOffsetHeader.entryCount ? input.Length : blockOffsets[i + 1].offset * BlockSize + BlockSize) - offset;
                var size = meta.magic == "PAK " ? calculatedSize : meta.size;

                var subStream = new SubStream(input, offset, size);
                var fileName = $"{i:00000000}{NlpSupport.DetermineExtension(meta)}";

                result.Add(new NlpArchiveFile(new ArchiveFileInfo
                {
                    FilePath = fileName,
                    FileData = subStream,
                    PluginIds = meta.magic == "PAK " ? [Guid.Parse("a4615fdf-f408-4d22-a3fe-17f082f974e0")] : null
                }, meta, blockOffset.metaId));
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFile> files)
        {
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var metaOffset = BlockSize;
            var blockOffset = metaOffset + _header.entryCount * MetaSize;
            var blockOffsetEnd = blockOffset + BlockOffsetHeaderSize + files.Count * BlockOffsetSize;
            var fileOffset = (blockOffsetEnd + (BlockSize - 1)) & ~(BlockSize - 1);

            // Write files
            var blockOffsets = new List<NlpBlockOffset>();
            var metas = Enumerable.Repeat(new NlpMeta(), _header.entryCount).ToArray();

            var filePosition = fileOffset;
            foreach (var file in files.Cast<NlpArchiveFile>())
            {
                // Write file
                output.Position = filePosition;
                var writtenSize = file.WriteFileData(output, true);
                bw.WriteAlignment(BlockSize);

                // Update meta entry
                AdjustMeta(file);
                metas[file.Id] = file.Meta;

                // Add block offset entry
                blockOffsets.Add(new NlpBlockOffset
                {
                    metaId = file.Id,
                    offset = (filePosition - BlockSize) / BlockSize
                });

                filePosition += (int)((writtenSize + (BlockSize - 1)) & ~(BlockSize - 1));
            }

            // Write metas
            output.Position = metaOffset;
            WriteMetas(metas, bw);

            // Write block offsets
            var offsetHeader = new NlpBlockOffsetHeader
            {
                entryCount = files.Count,
                offset = blockOffset + BlockOffsetHeaderSize - BlockSize
            };

            output.Position = blockOffset;
            WriteOffsetHeader(offsetHeader, bw);
            WriteOffsets(blockOffsets, bw);
            bw.WriteAlignment(BlockSize);

            // Write header
            output.Position = 0;

            _header.fileBlockOffset = (fileOffset - BlockSize) / BlockSize;
            _header.entryCount = metas.Length;
            _header.blockEntriesOffset = blockOffset - BlockSize;
            _header.unkOffset = blockOffsetEnd - BlockSize;
            WriteHeader(_header, bw);
        }

        private void AdjustMeta(NlpArchiveFile file)
        {
            var fileStream = file.GetFileData().Result;
            fileStream.Position = 0;

            using var br = new BinaryReaderX(fileStream, true);
            var magic = br.ReadString(4);

            // Since PACK files need special value settings, check if we deal with it
            if (magic == "PACK")
            {
                br.BaseStream.Position = 0x10;

                file.Meta.magic = "PAK ";
                file.Meta.dataStart = br.ReadInt32();
                file.Meta.size = br.ReadInt32();

                return;
            }

            // Otherwise set size and default values for other fields
            file.Meta.size = (int)file.FileSize;
            file.Meta.dataStart = 0;
            file.Meta.unk2 = 0;
        }

        private NlpHeader ReadHeader(BinaryReaderX reader)
        {
            return new NlpHeader
            {
                unk1 = reader.ReadInt32(),
                fileBlockOffset = reader.ReadInt32(),
                unk2 = reader.ReadInt32(),
                unk3 = reader.ReadInt32(),
                entryCount = reader.ReadInt32(),
                blockEntriesOffset = reader.ReadInt32(),
                unk4 = reader.ReadInt32(),
                unk5 = reader.ReadInt32(),
                unkCount = reader.ReadInt32(),
                unkOffset = reader.ReadInt32()
            };
        }

        private NlpMeta[] ReadMetas(BinaryReaderX reader, int count)
        {
            var result = new NlpMeta[count];

            for (var i = 0; i < count; i++)
                result[i] = ReadMeta(reader);

            return result;
        }

        private NlpMeta ReadMeta(BinaryReaderX reader)
        {
            return new NlpMeta
            {
                magic = reader.ReadString(4),
                zero0 = reader.ReadInt32(),
                size = reader.ReadInt32(),
                dataStart = reader.ReadInt32(),
                unk2 = reader.ReadInt32()
            };
        }

        private NlpBlockOffsetHeader ReadOffsetHeader(BinaryReaderX reader)
        {
            return new NlpBlockOffsetHeader
            {
                zero0 = reader.ReadInt32(),
                entryCount = reader.ReadInt32(),
                offset = reader.ReadInt32()
            };
        }

        private NlpBlockOffset[] ReadOffsets(BinaryReaderX reader, int count)
        {
            var result = new NlpBlockOffset[count];

            for (var i = 0; i < count; i++)
                result[i] = ReadOffset(reader);

            return result;
        }

        private NlpBlockOffset ReadOffset(BinaryReaderX reader)
        {
            return new NlpBlockOffset
            {
                metaId = reader.ReadInt32(),
                offset = reader.ReadInt32()
            };
        }

        private void WriteHeader(NlpHeader header, BinaryWriterX writer)
        {
            writer.Write(header.unk1);
            writer.Write(header.fileBlockOffset);
            writer.Write(header.unk2);
            writer.Write(header.unk3);
            writer.Write(header.entryCount);
            writer.Write(header.blockEntriesOffset);
            writer.Write(header.unk4);
            writer.Write(header.unk5);
            writer.Write(header.unkCount);
            writer.Write(header.unkOffset);
        }

        private void WriteMetas(IList<NlpMeta> metas, BinaryWriterX writer)
        {
            foreach (NlpMeta meta in metas)
                WriteMeta(meta, writer);
        }

        private void WriteMeta(NlpMeta entry, BinaryWriterX writer)
        {
            writer.WriteString(entry.magic, writeNullTerminator: false);
            writer.Write(entry.zero0);
            writer.Write(entry.size);
            writer.Write(entry.dataStart);
            writer.Write(entry.unk2);
        }

        private void WriteOffsetHeader(NlpBlockOffsetHeader header, BinaryWriterX writer)
        {
            writer.Write(header.zero0);
            writer.Write(header.entryCount);
            writer.Write(header.offset);
        }

        private void WriteOffsets(IList<NlpBlockOffset> offsets, BinaryWriterX writer)
        {
            foreach (NlpBlockOffset offset in offsets)
                WriteOffset(offset, writer);
        }

        private void WriteOffset(NlpBlockOffset offset, BinaryWriterX writer)
        {
            writer.Write(offset.metaId);
            writer.Write(offset.offset);
        }
    }
}
