using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Models.Archive;
using Kontract.Models.IO;

namespace plugin_sony.Archives.PSARC
{
    class PSARC
    {
        private static readonly int HeaderSize = Tools.MeasureType(typeof(PsarcHeader));

        private int BlockLength = 1;
        private List<int> CompressedBlockSizes = new List<int>();

        public const ushort ZLibHeader = 0x78DA;
        //public const ushort LzmaHeader = 0x????;
        public const ushort AllStarsEncryptionA = 0x0001;
        public const ushort AllStarsEncryptionB = 0x0002;

        public PsarcHeader PsarcHeader;
        public bool AllStarsEncryptedArchive;

        public List<IArchiveFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true, ByteOrder.BigEndian);
            PsarcHeader = br.ReadType<PsarcHeader>();

            // Determine block length
            uint blockIterator = 256;
            do
            {
                blockIterator *= 256;
                BlockLength = (ushort)(BlockLength + 1);
            } while (blockIterator < PsarcHeader.BlockSize);

            // Read manifest entry
            var manifestEntry = br.ReadType<PsarcEntry>();

            // Read file entries
            var fileEntries = br.ReadMultiple<PsarcEntry>(PsarcHeader.TocEntryCount - 1);

            // Blocks
            var numBlocks = (PsarcHeader.TocSize - (int)br.BaseStream.Position) / BlockLength;
            for (var i = 0; i < numBlocks; i++)
                CompressedBlockSizes.Add(br.ReadBytes(BlockLength).Reverse().Select((x, j) => x << 8 * j).Sum());

            // Check for SDAT Encryption
            if (PsarcHeader.TocEntryCount > 0)
            {
                br.BaseStream.Position = manifestEntry.Offset;
                var compression = br.ReadUInt16();
                br.BaseStream.Position -= 2;
                AllStarsEncryptedArchive = compression == AllStarsEncryptionA || compression == AllStarsEncryptionB;
            }

            // Load Filenames
            var filenames = new List<string>();
            if (!AllStarsEncryptedArchive)
            {
                var manifestStream = new PsarcBlockStream(input, manifestEntry, PsarcHeader.BlockSize, CompressedBlockSizes);
                using var brNames = new BinaryReaderX(manifestStream, Encoding.UTF8);
                for (var i = 1; i < PsarcHeader.TocEntryCount; i++)
                    filenames.Add(Encoding.UTF8.GetString(brNames.ReadBytesUntil(0x0, (byte)'\n')));
            }
            else
            {
                // Temporary until we can decrypt AllStars PSARCs.
                for (var i = 1; i < PsarcHeader.TocEntryCount; i++)
                    filenames.Add($"{i:00000000}.bin");
            }

            // Files
            var files = new List<IArchiveFileInfo>();
            if (!AllStarsEncryptedArchive)
            {
                for (int i = 0; i < fileEntries.Count; i++)
                    files.Add(new PsarcFileInfo(new PsarcBlockStream(input, fileEntries[i], PsarcHeader.BlockSize, CompressedBlockSizes), filenames[i]));
            }
            else
            {
                for (int i = 0; i < fileEntries.Count; i++)
                {
                    var entry = fileEntries[i];
                    var compressedSize = 0L;
                    for (var j = entry.FirstBlockIndex; j < entry.FirstBlockIndex + Math.Ceiling((double)entry.UncompressedSize / PsarcHeader.BlockSize); j++)
                        compressedSize += CompressedBlockSizes[j] == 0 ? PsarcHeader.BlockSize : CompressedBlockSizes[j];

                    files.Add(new ArchiveFileInfo(new SubStream(input, entry.Offset, compressedSize), filenames[i]));
                }
            }

            return files;
        }

        //public void Save(Stream output)
        //{
        //    // TODO: Saving... today.

        //    using (var bw = new BinaryWriterX(output, ByteOrder.BigEndian))
        //    {
        //        // Create Manifest
        //        var filePaths = new List<string>();
        //        for (var i = 1; i < PsarcHeader.TocEntryCount; i++)
        //        {
        //            var afi = _files[i];
        //            switch (PsarcHeader.ArchiveFlags)
        //            {
        //                case ArchiveFlags.RelativePaths:
        //                    filePaths.Add(afi.FileName.TrimStart('/'));
        //                    break;
        //                case ArchiveFlags.IgnoreCasePaths:
        //                case ArchiveFlags.AbsolutePaths:
        //                    filePaths.Add(afi.FileName);
        //                    break;
        //            }
        //        }
        //        var manifest = new MemoryStream(Encoding.ASCII.GetBytes(string.Join("\n", filePaths)));

        //        // Update Block Count and Size
        //        var compressedBlocksOffset = HeaderSize + PsarcHeader.TocEntryCount * PsarcHeader.TocEntrySize;
        //        var compressedBlockCount = 0;
        //        foreach (var afi in _files)
        //        {
        //            switch (afi.State)
        //            {
        //                case ArchiveFileState.Archived:
        //                case ArchiveFileState.Renamed:
        //                    compressedBlockCount += (int)Math.Ceiling((double)afi.PsarcEntry.UncompressedSize / PsarcHeader.BlockSize);
        //                    break;
        //                case ArchiveFileState.Added:
        //                case ArchiveFileState.Replaced:
        //                    compressedBlockCount += (int)Math.Ceiling((double)afi.FileData.Length / PsarcHeader.BlockSize);
        //                    break;
        //                case ArchiveFileState.Empty:
        //                case ArchiveFileState.Deleted:
        //                    break;
        //            }
        //        }
        //        bw.BaseStream.Position = PsarcHeader.TocSize = compressedBlocksOffset + compressedBlockCount * BlockLength;

        //        // Writing _files
        //        var compressedBlocks = new List<int>();
        //        var lastPosition = bw.BaseStream.Position;

        //        // Write Generated Manifest File
        //        WriteFile(bw, _files[0], null, compressedBlocks, ref lastPosition);

        //        // Write All Other _files
        //        for (var i = 1; i < PsarcHeader.TocEntryCount; i++)
        //            WriteFile(bw, _files[i], null, compressedBlocks, ref lastPosition);

        //        // Write Updated Entries
        //        bw.BaseStream.Position = HeaderSize;
        //        foreach (var psarcEntry in _files.Select(e => e.PsarcEntry))
        //        {
        //            bw.Write(psarcEntry.MD5Hash);
        //            bw.Write((uint)psarcEntry.FirstBlockIndex);
        //            bw.Write(BitConverter.GetBytes(psarcEntry.UncompressedSize).Take(5).Reverse().ToArray());
        //            bw.Write(BitConverter.GetBytes(psarcEntry.Offset).Take(5).Reverse().ToArray());
        //        }

        //        // Write Updated Compressed Blocks
        //        foreach (var block in compressedBlocks)
        //            bw.Write(BitConverter.GetBytes((uint)block).Take(BlockLength).Reverse().ToArray());

        //        // PsarcHeader
        //        bw.BaseStream.Position = 0;
        //        bw.WriteType(PsarcHeader);
        //    }
        //}

        //private void WriteFile(BinaryWriterX bw, PsarcFileInfo afi, Stream @override, List<int> compressedBlocks, ref long lastPosition)
        //{
        //    var psarcEntry = afi.PsarcEntry;

        //    if (afi.State == ArchiveFileState.Archived && @override == null)
        //    {
        //        var originalBlockIndex = psarcEntry.FirstBlockIndex;
        //        var originalOffset = psarcEntry.Offset;

        //        // Update PsarcEntry
        //        psarcEntry.FirstBlockIndex = compressedBlocks.Count;
        //        psarcEntry.Offset = lastPosition;

        //        // Write File Chunks and Add Blocks
        //        using (var br = new BinaryReaderX(afi.BaseFileData, true))
        //        {
        //            br.BaseStream.Position = originalOffset;
        //            for (var i = originalBlockIndex; i < originalBlockIndex + Math.Ceiling((double)psarcEntry.UncompressedSize / PsarcHeader.BlockSize); i++)
        //            {
        //                if (CompressedBlockSizes[i] == 0)
        //                    bw.Write(br.ReadBytes(PsarcHeader.BlockSize));
        //                else
        //                    bw.Write(br.ReadBytes(CompressedBlockSizes[i]));
        //                compressedBlocks.Add(CompressedBlockSizes[i]);
        //            }
        //            lastPosition = bw.BaseStream.Position;
        //        }
        //    }
        //    else
        //    {
        //        var input = @override ?? afi.FileData;

        //        // Update PsarcEntry
        //        psarcEntry.UncompressedSize = (int)input.Length;
        //        psarcEntry.FirstBlockIndex = compressedBlocks.Count;
        //        psarcEntry.Offset = lastPosition;

        //        // Write File Chunks and Add Blocks
        //        using (var br = new BinaryReaderX(input, true))
        //            for (var i = 0; i < Math.Ceiling((double)input.Length / PsarcHeader.BlockSize); i++)
        //            {
        //                if (PsarcHeader.Compression == "zlib")
        //                {
        //                    bw.Write(ZLibHeader);
        //                    var readLength = (int)Math.Min(PsarcHeader.BlockSize, br.BaseStream.Length - (PsarcHeader.BlockSize * i));
        //                    using (var ds = new DeflateStream(bw.BaseStream, CompressionLevel.Optimal, true))
        //                        ds.Write(br.ReadBytes(readLength), 0, readLength);
        //                }
        //                else if (PsarcHeader.Compression == "lzma")
        //                {
        //                    // TODO: Implement LZMA support when we find a file that uses it.
        //                }

        //                compressedBlocks.Add((int)(bw.BaseStream.Position - lastPosition));
        //                lastPosition = bw.BaseStream.Position;
        //            }
        //    }
        //}

        //public void Close()
        //{
        //    _stream?.Dispose();
        //    foreach (var afi in _files)
        //        if (afi.State != ArchiveFileState.Archived)
        //            afi.FileData?.Dispose();
        //    _stream = null;
        //}
    }
}
