using System.Collections.Generic;
using System.IO;
using System.Linq;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Models.Archive;

namespace plugin_lemon_interactive.Archives
{
    /// <summary>
    /// 
    /// </summary>
    public class Dpk4
    {
        /// <summary>
        /// 
        /// </summary>
        private static readonly int FileEntrySize = Tools.MeasureType(typeof(Dpk4FileEntry));

        /// <summary>
        /// 
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public IList<IArchiveFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            var header = br.ReadType<Dpk4Header>();
            var entries = br.ReadMultiple<Dpk4FileEntry>(header.fileCount);

            return entries.Select(e => CreateAfi(new SubStream(input, e.offset, e.compressedSize), e)).ToList<IArchiveFileInfo>();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="output"></param>
        /// <param name="files"></param>
        public void Save(Stream output, IList<IArchiveFileInfo> files)
        {
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var fileOffset = (4 + files.Count * FileEntrySize + 0x3F) & ~0x3F;

            // Write files
            var entries = new List<Dpk4FileEntry>();

            bw.BaseStream.Position = fileOffset;
            foreach (var file in files.Cast<ArchiveFileInfo>())
            {
                fileOffset = (int)bw.BaseStream.Position;
                var writtenSize = file.SaveFileData(output);

                if (file != files.Last())
                    bw.WriteAlignment(0x40);

                //var entry = new Dpk4FileEntry
                //{
                //    offset = fileOffset,
                //    size = (uint)writtenSize,
                //    compressedSize = (int)file.FileSize
                //};
                //if (file.UsesCompression)
                //    entry.size |= 0x80000000;

                //entries.Add(entry);
            }

            // Write entries
            bw.BaseStream.Position = 0;

            bw.Write(files.Count);
            bw.WriteMultiple(entries);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="file"></param>
        /// <param name="name"></param>
        /// <param name="entry"></param>
        /// <returns></returns>
        private ArchiveFileInfo CreateAfi(Stream file, Dpk4FileEntry entry)
        {
            if (entry.IsCompressed)
                return new ArchiveFileInfo(file, entry.fileName.Trim('\0'), Kompression.Implementations.Compressions.ZLib, entry.size);

            return new ArchiveFileInfo(file, entry.fileName.Trim('\0'));
        }
    }
}
