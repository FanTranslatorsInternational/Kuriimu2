﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Extensions;
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
            var headerSize = Tools.MeasureType(typeof(Dpk4Header));

            // Jump to initial file offset
            bw.BaseStream.Position = headerSize + files.Aggregate(0, (offset, file) => (offset + 16 + file.FilePath.ToRelative().FullName.Length + 1 + 3) & ~3);

            // Write files
            var entries = new List<Dpk4FileEntry>();
            foreach (var file in files.Cast<ArchiveFileInfo>())
            {
                var path = file.FilePath.ToRelative().FullName.Replace("/", "\\");
                var fileOffset = (int)bw.BaseStream.Position;
                var writtenSize = file.SaveFileData(bw.BaseStream);

                entries.Add(new Dpk4FileEntry
                {
                    entrySize = (16 + path.Length + 1 + 3) & ~3,
                    size = (int)file.FileSize,
                    compressedSize = (int)writtenSize,
                    offset = fileOffset,
                    fileName = path + "\0"
                });
            }

            // Create header
            var header = new Dpk4Header { fileSize = (uint)bw.BaseStream.Position };

            // Write entries
            bw.BaseStream.Position = headerSize;
            bw.WriteMultiple(entries);
            header.fileTableSize = (int)bw.BaseStream.Position - headerSize;
            header.fileCount = entries.Count;

            // Header
            bw.BaseStream.Position = 0;
            bw.WriteType(header);
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
