using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Komponent.IO;
using Komponent.IO.Streams;
using Kompression.Configuration;
using Kontract.Models.Archive;
using Kontract.Models.IO;

namespace plugin_sony.Archives.PSARC
{
    public sealed class PSARC
    {
        private const string ManifestName = "/psarc.manifest";
        private const int HeaderSize = 0x20;

        public const ushort ZLibHeader = 0x78DA;
        //public const ushort LzmaHeader = 0x????;
        public const ushort AllStarsEncryptionA = 0x0001;
        public const ushort AllStarsEncryptionB = 0x0002;

        private Stream _stream;

        public Header Header;
        private int BlockLength = 1;
        private List<int> CompressedBlockSizes = new List<int>();
        public bool AllStarsEncryptedArchive;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public List<IArchiveFileInfo> Load(Stream input)
        {
            _stream = input;

            var _files = new List<PsarcFileInfo>();

            using var br = new BinaryReaderX(input, ByteOrder.BigEndian);
            Header = br.ReadType<Header>();

            // Determine BlockLength
            uint blockIterator = 256;
            do
            {
                blockIterator *= 256;
                BlockLength = (ushort)(BlockLength + 1);
            } while (blockIterator < Header.BlockSize);

            // Entries
            for (var i = 0; i < Header.TocEntryCount; i++)
            {
                //var md5Hash = br.ReadBytes(16);
                //var firstBlockIndex = br.ReadInt32();
                //var length = br.ReadUInt64() >> 24;
                //br.BaseStream.Position -= 6;
                //var offset = br.ReadUInt64() & 0xFFFFFFFFFF;

                var entry = br.ReadType<Entry>();

                //var entry = new Entry
                //{
                //    MD5Hash = md5Hash,
                //    FirstBlockIndex = firstBlockIndex,
                //    UncompressedSize = (long)length,
                //    Offset = (long)offset
                //};

                //Math.Ceiling((double)entry.UncompressedSize / Header.BlockSize);

                var fileInfo = new PsarcFileInfo(i, entry, input, $"/{i:00000000}.bin");

                _files.Add(fileInfo);
            }

            // Manifest Filename
            if (_files.Count > 0)
                _files[0].FilePath = ManifestName;

            // Blocks
            var numBlocks = (Header.TocSize - (int)br.BaseStream.Position) / BlockLength;
            for (var i = 0; i < numBlocks; i++)
                CompressedBlockSizes.Add(br.ReadBytes(BlockLength).Reverse().Select((x, j) => x << 8 * j).Sum());

            // Check for SDAT Encryption
            if (_files.Count > 0)
            {
                br.BaseStream.Position = _files[0].Entry.Offset;
                var compression = br.ReadUInt16();
                br.BaseStream.Position -= 2;
                AllStarsEncryptedArchive = compression == AllStarsEncryptionA || compression == AllStarsEncryptionB;
            }

            // _files
            for (var i = 0; i < Header.TocEntryCount; i++)
            {
                _files[i].BlockSize = Header.BlockSize;
                _files[i].BlockSizes = CompressedBlockSizes;
                //_files[i].FileData = br.BaseStream;
            }

            // Load Filenames
            if (!AllStarsEncryptedArchive)
            {
                using var brNames = new BinaryReaderX(_files[0].GetFileData().Result, Encoding.UTF8);
                for (var i = 1; i < Header.TocEntryCount; i++)
                    _files[i].FilePath = Encoding.UTF8.GetString(brNames.ReadBytesUntil((byte)'\n')) ?? _files[i].FilePath;
            }
            else
            {
                for (var i = 1; i < Header.TocEntryCount; i++)
                    _files[i].FilePath = "/" + i.ToString("00000000") + ".bin";
            }

            return _files.Cast<IArchiveFileInfo>().ToList();
        }

        //public void Save(Stream output)
        //{
        //    // TODO: Saving... today.

        //    using (var bw = new BinaryWriterX(output, ByteOrder.BigEndian))
        //    {
        //        // Create Manifest
        //        var filePaths = new List<string>();
        //        for (var i = 1; i < Header.TocEntryCount; i++)
        //        {
        //            var afi = _files[i];
        //            switch (Header.ArchiveFlags)
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
        //        var compressedBlocksOffset = HeaderSize + Header.TocEntryCount * Header.TocEntrySize;
        //        var compressedBlockCount = 0;
        //        foreach (var afi in _files)
        //        {
        //            switch (afi.State)
        //            {
        //                case ArchiveFileState.Archived:
        //                case ArchiveFileState.Renamed:
        //                    compressedBlockCount += (int)Math.Ceiling((double)afi.Entry.UncompressedSize / Header.BlockSize);
        //                    break;
        //                case ArchiveFileState.Added:
        //                case ArchiveFileState.Replaced:
        //                    compressedBlockCount += (int)Math.Ceiling((double)afi.FileData.Length / Header.BlockSize);
        //                    break;
        //                case ArchiveFileState.Empty:
        //                case ArchiveFileState.Deleted:
        //                    break;
        //            }
        //        }
        //        bw.BaseStream.Position = Header.TocSize = compressedBlocksOffset + compressedBlockCount * BlockLength;

        //        // Writing _files
        //        var compressedBlocks = new List<int>();
        //        var lastPosition = bw.BaseStream.Position;

        //        // Write Generated Manifest File
        //        WriteFile(bw, _files[0], null, compressedBlocks, ref lastPosition);

        //        // Write All Other _files
        //        for (var i = 1; i < Header.TocEntryCount; i++)
        //            WriteFile(bw, _files[i], null, compressedBlocks, ref lastPosition);

        //        // Write Updated Entries
        //        bw.BaseStream.Position = HeaderSize;
        //        foreach (var entry in _files.Select(e => e.Entry))
        //        {
        //            bw.Write(entry.MD5Hash);
        //            bw.Write((uint)entry.FirstBlockIndex);
        //            bw.Write(BitConverter.GetBytes(entry.UncompressedSize).Take(5).Reverse().ToArray());
        //            bw.Write(BitConverter.GetBytes(entry.Offset).Take(5).Reverse().ToArray());
        //        }

        //        // Write Updated Compressed Blocks
        //        foreach (var block in compressedBlocks)
        //            bw.Write(BitConverter.GetBytes((uint)block).Take(BlockLength).Reverse().ToArray());

        //        // Header
        //        bw.BaseStream.Position = 0;
        //        bw.WriteType(Header);
        //    }
        //}

        //private void WriteFile(BinaryWriterX bw, PsarcFileInfo afi, Stream @override, List<int> compressedBlocks, ref long lastPosition)
        //{
        //    var entry = afi.Entry;

        //    if (afi.State == ArchiveFileState.Archived && @override == null)
        //    {
        //        var originalBlockIndex = entry.FirstBlockIndex;
        //        var originalOffset = entry.Offset;

        //        // Update Entry
        //        entry.FirstBlockIndex = compressedBlocks.Count;
        //        entry.Offset = lastPosition;

        //        // Write File Chunks and Add Blocks
        //        using (var br = new BinaryReaderX(afi.BaseFileData, true))
        //        {
        //            br.BaseStream.Position = originalOffset;
        //            for (var i = originalBlockIndex; i < originalBlockIndex + Math.Ceiling((double)entry.UncompressedSize / Header.BlockSize); i++)
        //            {
        //                if (CompressedBlockSizes[i] == 0)
        //                    bw.Write(br.ReadBytes(Header.BlockSize));
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

        //        // Update Entry
        //        entry.UncompressedSize = (int)input.Length;
        //        entry.FirstBlockIndex = compressedBlocks.Count;
        //        entry.Offset = lastPosition;

        //        // Write File Chunks and Add Blocks
        //        using (var br = new BinaryReaderX(input, true))
        //            for (var i = 0; i < Math.Ceiling((double)input.Length / Header.BlockSize); i++)
        //            {
        //                if (Header.Compression == "zlib")
        //                {
        //                    bw.Write(ZLibHeader);
        //                    var readLength = (int)Math.Min(Header.BlockSize, br.BaseStream.Length - (Header.BlockSize * i));
        //                    using (var ds = new DeflateStream(bw.BaseStream, CompressionLevel.Optimal, true))
        //                        ds.Write(br.ReadBytes(readLength), 0, readLength);
        //                }
        //                else if (Header.Compression == "lzma")
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
