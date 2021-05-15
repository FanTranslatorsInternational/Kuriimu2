using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Extensions;
using Kontract.Models.Archive;
using Kontract.Models.IO;
using Kryptography.Hash.Fnv;

namespace plugin_nintendo.Archives
{
    class Pac
    {
        private static readonly int HeaderSize = Tools.MeasureType(typeof(PacHeader));
        private static readonly int TableInfoSize = Tools.MeasureType(typeof(PacTableInfo));
        private static readonly int AssetSize = Tools.MeasureType(typeof(PacAsset));
        private static readonly int EntrySize = Tools.MeasureType(typeof(PacEntry));

        private PacHeader _header;

        public IList<IArchiveFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true, ByteOrder.BigEndian);

            // Read header
            _header = br.ReadType<PacHeader>();

            // Read table info
            var tableInfo = br.ReadType<PacTableInfo>();

            // Read assets
            input.Position = tableInfo.assetOffset;
            var assets = br.ReadMultiple<PacAsset>(tableInfo.assetCount);

            // Read entries
            input.Position = tableInfo.entryOffset;
            var entries = br.ReadMultiple<PacEntry>(tableInfo.entryCount);

            // Add files
            var result = new List<IArchiveFileInfo>();
            foreach (var asset in assets)
            {
                input.Position = asset.stringOffset;
                var assetName = br.ReadCStringASCII();

                var entryStartCount = (asset.entryOffset - tableInfo.entryOffset) / EntrySize;
                foreach (var entry in entries.Skip(entryStartCount).Take(asset.count))
                {
                    input.Position = entry.stringOffset;
                    var entryName = br.ReadCStringASCII();

                    var subStream = new SubStream(input, entry.offset, entry.compSize);
                    var fileName = assetName + "/" + entryName;

                    result.Add(new PacArchiveFileInfo(subStream, fileName, entry, Kompression.Implementations.Compressions.ZLib, entry.decompSize));
                }
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFileInfo> files)
        {
            using var bw = new BinaryWriterX(output, ByteOrder.BigEndian);
            var hash = Fnv1.Create();

            // Get distinct strings
            var stringMap = GetStringMap(files);

            // Calculate offsets
            var tableInfoOffset = HeaderSize;
            var assetOffset = (tableInfoOffset + TableInfoSize + 0x3F) & ~0x3F;
            var entryOffset = (assetOffset + files.Select(x => x.FilePath.GetFirstDirectory(out _)).Distinct().Count() * AssetSize + 0x3F) & ~0x3F;
            var stringOffset = (entryOffset + files.Count * EntrySize + 0x3F) & ~0x3F;
            var fileOffset = (stringOffset + stringMap.Sum(x => x.Key.Length + 1) + 0x3F) & ~0x3F;

            // Write files
            var entries = new List<PacEntry>();
            var fileMap = new Dictionary<uint, (long, long)>();
            var distinctFileCount = 0;

            var filePosition = fileOffset;
            foreach (var file in files.Cast<PacArchiveFileInfo>().OrderBy(x => x.FilePath))
            {
                // Update entry data
                file.FilePath.ToRelative().GetFirstDirectory(out var filePath);

                file.Entry.decompSize = (int)file.FileSize;
                file.Entry.extensionOffset = (int)stringMap[file.FilePath.GetExtensionWithDot()] + stringOffset;
                file.Entry.extensionFnvHash = hash.ComputeValue(file.FilePath.GetExtensionWithDot());
                file.Entry.stringOffset = (int)stringMap[filePath.FullName] + stringOffset;
                file.Entry.fnvHash = hash.ComputeValue(filePath.FullName);

                // Check if file already exists
                var fileHash = file.GetHash();
                if (fileMap.ContainsKey(fileHash))
                {
                    file.Entry.offset = (int)fileMap[fileHash].Item1;
                    file.Entry.compSize = file.Entry.compSize2 = (int)fileMap[fileHash].Item2;

                    entries.Add(file.Entry);
                    continue;
                }

                // Write file data
                output.Position = filePosition;
                var writtenSize = file.SaveFileData(output);

                file.Entry.offset = filePosition;
                file.Entry.compSize = file.Entry.compSize2 = (int)writtenSize;

                entries.Add(file.Entry);
                fileMap[fileHash] = (filePosition, writtenSize);
                distinctFileCount++;

                filePosition += (int) writtenSize;
            }
            bw.WriteAlignment();

            // Write strings
            output.Position = stringOffset;
            foreach (var pair in stringMap)
                bw.WriteString(pair.Key, Encoding.ASCII, false);

            // Write entries
            output.Position = entryOffset;
            bw.WriteMultiple(entries);

            // Write assets
            var entryPosition = entryOffset;
            var assetCount = 0;

            output.Position = assetOffset;
            foreach (var fileGroup in files.OrderBy(x => x.FilePath).GroupBy(x => x.FilePath.GetFirstDirectory(out _)))
            {
                var fileCount = fileGroup.Count();
                bw.WriteType(new PacAsset
                {
                    count = fileCount,
                    entryOffset = entryPosition,
                    stringOffset = (int)stringMap[fileGroup.Key] + stringOffset,
                    fnvHash = hash.ComputeValue(fileGroup.Key)
                });

                entryPosition += fileCount * EntrySize;
                assetCount++;
            }

            // Write table info
            output.Position = tableInfoOffset;
            bw.WriteType(new PacTableInfo
            {
                fileOffset = fileOffset,
                entryOffset = entryOffset,
                stringOffset = stringOffset,
                assetOffset = assetOffset,
                unpaddedFileSize = (int)output.Length,
                fileCount = distinctFileCount,
                entryCount = entries.Count,
                stringCount = stringMap.Count,
                assetCount = assetCount
            });

            // Write header
            output.Position = 0;

            _header.dataOffset = fileOffset;
            bw.WriteType(_header);

            // Pad file to 0x1000
            output.Position = output.Length;
            bw.WriteAlignment(0x1000);
        }

        private IDictionary<string, long> GetStringMap(IList<IArchiveFileInfo> files)
        {
            var strings = files.Select(x =>
             {
                 x.FilePath.ToRelative().GetFirstDirectory(out var remaining);
                 return remaining.FullName;
             }).Distinct();
            strings = strings.Concat(files.Select(x => x.FilePath.GetExtensionWithDot()).Distinct());
            strings = strings.Concat(files.Select(x => x.FilePath.GetFirstDirectory(out _)).Distinct());

            var stringPosition = 0;
            var stringMap = new Dictionary<string, long>();
            foreach (var str in strings)
            {
                stringMap[str] = stringPosition;
                stringPosition += str.Length + 1;
            }

            return stringMap;
        }
    }
}
