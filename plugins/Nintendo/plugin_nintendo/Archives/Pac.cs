using System.Text;
using Komponent.Contract.Enums;
using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Extensions;
using Kryptography.Checksum.Fnv;

namespace plugin_nintendo.Archives
{
    class Pac
    {
        private const int HeaderSize_ = 0x10;
        private const int TableInfoSize_ = 0x34;
        private const int AssetSize_ = 0x10;
        private const int EntrySize_ = 0x30;

        private PacHeader _header;

        public List<IArchiveFile> Load(Stream input)
        {
            var hash = Fnv1.Create();

            using var br = new BinaryReaderX(input, true, ByteOrder.BigEndian);

            // Read header
            _header = ReadHeader(br);

            // Read table info
            var tableInfo = ReadTableInfo(br);

            // Read assets
            input.Position = tableInfo.assetOffset;
            var assets = ReadAssets(br, tableInfo.assetCount);

            // Read entries
            input.Position = tableInfo.entryOffset;
            var entries = ReadEntries(br, tableInfo.entryCount);

            // Add files
            var result = new List<IArchiveFile>();
            foreach (var asset in assets)
            {
                input.Position = asset.stringOffset;
                var assetName = br.ReadNullTerminatedString();

                var entryStartCount = (asset.entryOffset - tableInfo.entryOffset) / EntrySize_;
                foreach (var entry in entries.Skip(entryStartCount).Take(asset.count))
                {
                    input.Position = entry.stringOffset;
                    var entryName = br.ReadNullTerminatedString();

                    var subStream = new SubStream(input, entry.offset, entry.compSize);
                    var fileName = assetName + "/" + entryName;

                    result.Add(new PacArchiveFile(new CompressedArchiveFileInfo
                    {
                        FilePath = fileName,
                        FileData = subStream,
                        Compression = Kompression.Compressions.ZLib.Build(),
                        DecompressedSize = entry.decompSize
                    }, entry));
                }
            }

            return result;
        }

        public void Save(Stream output, List<IArchiveFile> files)
        {
            using var bw = new BinaryWriterX(output, ByteOrder.BigEndian);

            var hash = Fnv1.Create();

            // Get distinct strings
            var stringMap = GetStringMap(files);

            // Calculate offsets
            var tableInfoOffset = HeaderSize_;
            var assetOffset = (tableInfoOffset + TableInfoSize_ + 0x3F) & ~0x3F;
            var entryOffset = (assetOffset + files.Select(x => x.FilePath.GetFirstDirectory(out _)).Distinct().Count() * AssetSize_ + 0x3F) & ~0x3F;
            var stringOffset = (entryOffset + files.Count * EntrySize_ + 0x3F) & ~0x3F;
            var fileOffset = (stringOffset + stringMap.Sum(x => x.Key.Length + 1) + 0x3F) & ~0x3F;

            // Write files
            var entries = new List<PacEntry>();
            var fileMap = new Dictionary<uint, (long, long)>();
            var distinctFileCount = 0;

            var filePosition = fileOffset;
            foreach (var file in files.Cast<PacArchiveFile>().OrderBy(x => x.FilePath))
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
                var writtenSize = file.WriteFileData(output, true);

                file.Entry.offset = filePosition;
                file.Entry.compSize = file.Entry.compSize2 = (int)writtenSize;

                entries.Add(file.Entry);
                fileMap[fileHash] = (filePosition, writtenSize);
                distinctFileCount++;

                filePosition += (int)writtenSize;
            }
            bw.WriteAlignment(16);

            // Write strings
            output.Position = stringOffset;
            foreach (var pair in stringMap)
                bw.WriteString(pair.Key, Encoding.ASCII);

            // Write entries
            output.Position = entryOffset;
            WriteEntries(entries, bw);

            // Write assets
            var entryPosition = entryOffset;
            var assetCount = 0;

            output.Position = assetOffset;
            foreach (var fileGroup in files.OrderBy(x => x.FilePath).GroupBy(x => x.FilePath.GetFirstDirectory(out _)))
            {
                var fileCount = fileGroup.Count();
                var asset = new PacAsset
                {
                    count = fileCount,
                    entryOffset = entryPosition,
                    stringOffset = (int)stringMap[fileGroup.Key] + stringOffset,
                    fnvHash = hash.ComputeValue(fileGroup.Key)
                };

                WriteAsset(asset, bw);

                entryPosition += fileCount * EntrySize_;
                assetCount++;
            }

            // Write table info
            var tableInfo = new PacTableInfo
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
            };

            output.Position = tableInfoOffset;
            WriteTableInfo(tableInfo, bw);

            // Write header
            output.Position = 0;

            _header.dataOffset = fileOffset;
            WriteHeader(_header, bw);

            // Pad file to 0x1000
            output.Position = output.Length;
            bw.WriteAlignment(0x1000);
        }

        private IDictionary<string, long> GetStringMap(List<IArchiveFile> files)
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

        private PacHeader ReadHeader(BinaryReaderX reader)
        {
            return new PacHeader
            {
                magic = reader.ReadString(4),
                unk1 = reader.ReadInt32(),
                unk2 = reader.ReadInt32(),
                dataOffset = reader.ReadInt32()
            };
        }

        private PacTableInfo ReadTableInfo(BinaryReaderX reader)
        {
            return new PacTableInfo
            {
                unpaddedFileSize = reader.ReadInt32(),
                assetCount = reader.ReadInt32(),
                entryCount = reader.ReadInt32(),
                stringCount = reader.ReadInt32(),
                fileCount = reader.ReadInt32(),
                zero0 = reader.ReadInt64(),
                zero1 = reader.ReadInt64(),
                assetOffset = reader.ReadInt32(),
                entryOffset = reader.ReadInt32(),
                stringOffset = reader.ReadInt32(),
                fileOffset = reader.ReadInt32(),
            };
        }

        private PacAsset[] ReadAssets(BinaryReaderX reader, int count)
        {
            var result = new PacAsset[count];

            for (var i = 0; i < count; i++)
                result[i] = ReadAsset(reader);

            return result;
        }

        private PacAsset ReadAsset(BinaryReaderX reader)
        {
            return new PacAsset
            {
                stringOffset = reader.ReadInt32(),
                fnvHash = reader.ReadUInt32(),
                count = reader.ReadInt32(),
                entryOffset = reader.ReadInt32()
            };
        }

        private PacEntry[] ReadEntries(BinaryReaderX reader, int count)
        {
            var result = new PacEntry[count];

            for (var i = 0; i < count; i++)
                result[i] = ReadEntry(reader);

            return result;
        }

        private PacEntry ReadEntry(BinaryReaderX reader)
        {
            return new PacEntry
            {
                stringOffset = reader.ReadInt32(),
                fnvHash = reader.ReadUInt32(),
                extensionOffset = reader.ReadInt32(),
                extensionFnvHash = reader.ReadUInt32(),
                offset = reader.ReadInt32(),
                decompSize = reader.ReadInt32(),
                compSize = reader.ReadInt32(),
                compSize2 = reader.ReadInt32(),
                zero0 = reader.ReadInt64(),
                unk1 = reader.ReadInt32(),
                zero1 = reader.ReadInt32()
            };
        }

        private void WriteHeader(PacHeader header, BinaryWriterX writer)
        {
            writer.WriteString(header.magic, writeNullTerminator: false);
            writer.Write(header.unk1);
            writer.Write(header.unk2);
            writer.Write(header.dataOffset);
        }

        private void WriteTableInfo(PacTableInfo tableInfo, BinaryWriterX writer)
        {
            writer.Write(tableInfo.unpaddedFileSize);
            writer.Write(tableInfo.assetCount);
            writer.Write(tableInfo.entryCount);
            writer.Write(tableInfo.stringCount);
            writer.Write(tableInfo.fileCount);
            writer.Write(tableInfo.zero0);
            writer.Write(tableInfo.zero1);
            writer.Write(tableInfo.assetOffset);
            writer.Write(tableInfo.entryOffset);
            writer.Write(tableInfo.stringOffset);
            writer.Write(tableInfo.fileOffset);
        }

        private void WriteAsset(PacAsset asset, BinaryWriterX writer)
        {
            writer.Write(asset.stringOffset);
            writer.Write(asset.fnvHash);
            writer.Write(asset.count);
            writer.Write(asset.entryOffset);
        }

        private void WriteEntries(IList<PacEntry> entries, BinaryWriterX writer)
        {
            foreach (PacEntry entry in entries)
                WriteEntry(entry, writer);
        }

        private void WriteEntry(PacEntry entry, BinaryWriterX writer)
        {
            writer.Write(entry.stringOffset);
            writer.Write(entry.fnvHash);
            writer.Write(entry.extensionOffset);
            writer.Write(entry.extensionFnvHash);
            writer.Write(entry.offset);
            writer.Write(entry.decompSize);
            writer.Write(entry.compSize);
            writer.Write(entry.compSize2);
            writer.Write(entry.zero0);
            writer.Write(entry.unk1);
            writer.Write(entry.zero1);
        }
    }
}
