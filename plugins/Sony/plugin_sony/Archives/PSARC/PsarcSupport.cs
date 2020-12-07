using Komponent.IO.Attributes;
using Kontract.Models.Archive;

namespace plugin_sony.Archives.PSARC
{
    /// <summary>
    /// 
    /// </summary>
    public class PsarcFileInfo : ArchiveFileInfo
    {
        /// <summary>
        /// 
        /// </summary>
        public override long FileSize => ((PsarcBlockStream)FileData).Entry.UncompressedSize;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileData"></param>
        /// <param name="filePath"></param>
        public PsarcFileInfo(PsarcBlockStream fileData, string filePath) : base(fileData, filePath) { }
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

    /// <summary>
    /// 
    /// </summary>
    public enum ArchiveFlags
    {
        RelativePaths = 0,
        IgnoreCasePaths = 1,
        AbsolutePaths = 2
    }
}
