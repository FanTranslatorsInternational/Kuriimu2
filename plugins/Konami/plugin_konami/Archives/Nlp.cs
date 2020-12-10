using System.Collections.Generic;
using System.IO;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Models.Archive;
using System.Linq;

namespace plugin_konami.Archives
{
    class Nlp
    {
        private const int BlockSize = 0x800;

        private static readonly int MetaSize = Tools.MeasureType(typeof(NlpMeta));
        private static readonly int BlockOffsetHeaderSize = Tools.MeasureType(typeof(NlpBlockOffsetHeader));
        private static readonly int BlockOffsetSize = Tools.MeasureType(typeof(NlpBlockOffset));

        private NlpHeader _header;

        public IList<IArchiveFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            _header = br.ReadType<NlpHeader>();

            // Read meta
            input.Position = BlockSize;
            var metas = br.ReadMultiple<NlpMeta>(_header.entryCount);

            // Read block offsets
            input.Position = _header.blockEntriesOffset + BlockSize;
            var blockOffsetHeader = br.ReadType<NlpBlockOffsetHeader>();
            var blockOffsets = br.ReadMultiple<NlpBlockOffset>(blockOffsetHeader.entryCount);

            // Add files
            var result = new List<IArchiveFileInfo>();
            for (var i = 0; i < blockOffsetHeader.entryCount; i++)
            {
                var blockOffset = blockOffsets[i];
                var meta = metas[blockOffset.metaId];

                var offset = blockOffset.offset * BlockSize + BlockSize;
                var calculatedSize = (i + 1 == blockOffsetHeader.entryCount ? input.Length : blockOffsets[i + 1].offset * BlockSize + BlockSize) - offset;
                var size = meta.magic == "PAK " ? calculatedSize : meta.size;

                var subStream = new SubStream(input, offset, size);
                var fileName = $"{i:00000000}{NlpSupport.DetermineExtension(meta)}";

                result.Add(new NlpArchiveFileInfo(subStream, fileName, meta, blockOffset.metaId));
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFileInfo> files)
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
            foreach (var file in files.Cast<NlpArchiveFileInfo>())
            {
                // Write file
                output.Position = filePosition;
                var writtenSize = file.SaveFileData(output);
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
            bw.WriteMultiple(metas);

            // Write block offsets
            output.Position = blockOffset;
            bw.WriteType(new NlpBlockOffsetHeader
            {
                entryCount = files.Count,
                offset = blockOffset + BlockOffsetHeaderSize - BlockSize
            });
            bw.WriteMultiple(blockOffsets);
            bw.WriteAlignment(BlockSize);

            // Write header
            output.Position = 0;

            _header.fileBlockOffset = (fileOffset - BlockSize) / BlockSize;
            _header.entryCount = metas.Length;
            _header.blockEntriesOffset = blockOffset - BlockSize;
            _header.unkOffset = blockOffsetEnd - BlockSize;
            bw.WriteType(_header);
        }

        private void AdjustMeta(NlpArchiveFileInfo file)
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
    }
}
