using System.Collections.Generic;
using System.IO;
using System.Linq;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Models.Archive;

namespace plugin_level5.Archives
{
    // TODO:
    // Game: Time Travelers
    class Pck
    {
        private readonly int _entrySize = Tools.MeasureType(typeof(PckFileInfo));

        public IReadOnlyList<ArchiveFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read file infos
            var fileCount = br.ReadInt32();
            var entries = br.ReadMultiple<PckFileInfo>(fileCount);

            // Add files
            var result = new List<ArchiveFileInfo>();
            foreach (var entry in entries)
            {
                br.BaseStream.Position = entry.fileOffset;

                // Read hash block before the file data
                var blockOffset = 0;
                var entryHashes = (List<uint>)null;
                var hashIdent = br.ReadInt16();
                if (hashIdent == 0x64)
                {
                    var hashCount = br.ReadInt16();
                    entryHashes = br.ReadMultiple<uint>(hashCount);

                    blockOffset = (hashCount + 1) * 4;
                }

                // Decide filename
                var fileName = $"{entry.hash:X8}.bin";

                // Add file
                var fileStream = new SubStream(input, entry.fileOffset + blockOffset, entry.fileLength - blockOffset);
                result.Add(new PckArchiveFileInfo(fileStream, fileName, entry, entryHashes));
            }

            return result;

            //using (var br = new BinaryReaderX(input, true))
            //{
            //    var entries = br.ReadMultiple<Entry>(br.ReadInt32()).ToList();

            //    var dict = (dicts != null) ? dicts.Where(d => d.pckName == Path.GetFileNameWithoutExtension(filename)).ToList() : new List<Dict>();
            //    Files = entries.Select(entry =>
            //    {
            //        br.BaseStream.Position = entry.fileOffset;
            //        var hashes = (br.ReadInt16() == 0x64) ? br.ReadMultiple<uint>(br.ReadInt16()).ToList() : null;
            //        int blockOffset = hashes?.Count + 1 ?? 0;

            //        return new PckFileInfo
            //        {
            //            FileData = new SubStream(
            //                input,
            //                entry.fileOffset + blockOffset * 4,
            //                entry.fileLength - blockOffset * 4),
            //            FileName = (dict.Count > 0) ?
            //                (dict[0].keyValuePairs.Find(kvp => kvp.key == entry.hash) != null ?
            //                    dict[0].keyValuePairs.Find(kvp => kvp.key == entry.hash).value :
            //                    $"0x{entry.hash:X8}.bin") :
            //                $"0x{entry.hash:X8}.bin",
            //            State = ArchiveFileState.Archived,
            //            Entry = entry,
            //            Hashes = hashes
            //        };
            //    }).ToList();
            //}
        }

        public void Save(Stream output, IReadOnlyList<ArchiveFileInfo> files)
        {
            using var bw = new BinaryWriterX(output);

            // Write file count
            bw.Write(files.Count);

            // Write file infos
            var dataOffset = 4 + files.Count * _entrySize;
            foreach (var file in files.Cast<PckArchiveFileInfo>())
            {
                var fileSize = (int)file.FileSize;
                if (file.Hashes != null)
                    fileSize += (file.Hashes.Count + 1) * 4;

                bw.WriteType(new PckFileInfo
                {
                    hash = file.Entry.hash,
                    fileOffset = dataOffset,
                    fileLength = fileSize
                });

                dataOffset += fileSize;
            }

            // Write file data
            foreach (var afi in files.Cast<PckArchiveFileInfo>())
            {
                if (afi.Hashes != null)
                {
                    bw.Write((short)0x64);
                    bw.Write((short)afi.Hashes.Count);
                    bw.WriteMultiple(afi.Hashes);
                }

                afi.SaveFileData(bw.BaseStream, null);
            }
        }
    }
}
