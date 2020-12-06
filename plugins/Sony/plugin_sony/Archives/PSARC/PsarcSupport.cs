using System.Collections.Generic;
using System.IO;
using Komponent.IO.Attributes;
using Kontract.Models.Archive;

namespace plugin_sony.Archives.PSARC
{
    /// <summary>
    /// 
    /// </summary>
    public class PsarcFileInfo : ArchiveFileInfo
    {
        public int ID { get; }

        public Entry Entry { get; set; }

        public int BlockSize { get; set; }

        public List<int> BlockSizes { get; set; } = new List<int>();

        //public override Stream FileData
        //{
        //    get
        //    {
        //        if (State != ArchiveFileState.Archived) return base.FileData;

        //        var ms = new MemoryStream();
        //        using (var br = new BinaryReaderX(base.FileData, ByteOrder.BigEndian))
        //        {
        //            br.BaseStream.Position = Entry.Offset;

        //            for (var i = Entry.FirstBlockIndex; i < Entry.FirstBlockIndex + Math.Ceiling((double)Entry.UncompressedSize / BlockSize); i++)
        //            {
        //                if (BlockSizes[i] == 0)
        //                    ms.Write(br.ReadBytes(BlockSize), 0, BlockSize);
        //                else
        //                {
        //                    var compression = br.ReadUInt16();
        //                    br.BaseStream.Position -= 2;

        //                    var blockStart = br.BaseStream.Position;
        //                    if (compression == PSARC.ZLibHeader)
        //                    {
        //                        br.BaseStream.Position += 2;
        //                        using (var ds = new DeflateStream(br.BaseStream, CompressionMode.Decompress, true))
        //                            ds.CopyTo(ms);
        //                        br.BaseStream.Position = blockStart + BlockSizes[i];
        //                    }
        //                    // TODO: Add LZMA decompression support
        //                    else
        //                        ms.Write(br.ReadBytes(BlockSizes[i]), 0, BlockSizes[i]);
        //                }
        //            }
        //        }
        //        ms.Position = 0;

        //        return ms;
        //    }
        //}

        public Stream BaseFileData => base.FileData;

        //public override long FileSize => Entry.UncompressedSize;

        public PsarcFileInfo(int id, Entry entry, Stream fileData, string filePath) : base(fileData, filePath)
        {
            ID = id;
            Entry = entry;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class Header
    {
        [FixedLength(4)]
        public string Magic;
        public ushort Major;
        public ushort Minor;
        [FixedLength(4)]
        public string Compression;
        public int TocSize; // zSize
        public int TocEntrySize;
        public int TocEntryCount;
        public int BlockSize;
        public ArchiveFlags ArchiveFlags;

        public string Version => $"v{Major}.{Minor}";
    }

    /// <summary>
    /// 
    /// </summary>
    public sealed class Entry
    {
        [FixedLength(16)]
        public byte[] MD5Hash;
        public int FirstBlockIndex;
        [BitField(40)]
        public long UncompressedSize; // 40 bit (5 bytes)
        [BitField(40)]
        public long Offset; // 40 bit (5 bytes)
    }

    public enum ArchiveFlags
    {
        RelativePaths = 0,
        IgnoreCasePaths = 1,
        AbsolutePaths = 2
    }
}
