using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Models.Archive;
using Kryptography.Hash.Crc;
using plugin_level5.Compression;

namespace plugin_level5.Archives
{
    // TODO: Research Arc0 further
    class Arc0
    {
        private Arc0Header _header;

        private Stream _table1;
        private Stream _table2;

        private IList<Arc0FileEntry> _entries;

        private Stream _nameComp;

        public IReadOnlyList<ArchiveFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            _header = br.ReadType<Arc0Header>();

            // Read table 1
            _table1 = new SubStream(input, _header.offset1, _header.offset2 - _header.offset1);

            // Read table 2
            _table2 = new SubStream(input, _header.offset2, _header.fileEntriesOffset - _header.offset2);

            // Read file entry table
            var entryStreamComp = new SubStream(input, _header.nameOffset, _header.nameOffset - _header.fileEntriesOffset);
            var entryStream = new MemoryStream();
            Compressor.Decompress(entryStreamComp, entryStream);

            entryStream.Position = 0;
            var entryBr = new BinaryReaderX(entryStream);
            _entries = entryBr.ReadMultiple<Arc0FileEntry>(_header.fileEntriesCount);

            // Read nameTable
            _nameComp = new SubStream(input, _header.nameOffset, _header.dataOffset - _header.nameOffset);
            var nameStream = new MemoryStream();
            Compressor.Decompress(_nameComp, nameStream);

            nameStream.Position = 0;

            // Add Files
            var crc32 = Crc32.Create(Crc32Formula.Normal);
            var sjis = Encoding.GetEncoding("SJIS");

            var offsets = new List<uint>();
            var result = new List<ArchiveFileInfo>();
            foreach (var name in GetFileNames(nameStream))
            {
                var nameArray = sjis.GetBytes(name.Split('/').Last());
                var hash = ToUInt32LittleEndian(crc32.Compute(nameArray));
                var entry = _entries.First(c => c.crc32 == hash && !offsets.Contains(c.fileOffset));

                offsets.Add(entry.fileOffset);

                var fileData = new SubStream(input, _header.dataOffset + entry.fileOffset, entry.fileSize);
                result.Add(new Arc0ArchiveFileInfo(fileData, name, entry));
            }

            return result;
        }

        public void Save(Stream output, IReadOnlyList<ArchiveFileInfo> files)
        {
            var castedFiles = files.Cast<Arc0ArchiveFileInfo>().ToArray();
            using var bw = new BinaryWriterX(output);

            var dataOffset = (0x48 + _table1.Length + _table2.Length + _nameComp.Length + _entries.Count * 0x10 + 4 + 0x4) & ~0x4;

            output.Position = 0x48;

            // Write table 1
            _table1.Position = 0;
            _table1.CopyTo(output);

            // Write table 2
            _table2.Position = 0;
            _table2.CopyTo(output);

            // Write fileEntries Table
            bw.Write((_entries.Count * 0x10) << 3);

            uint offset = 0;
            var fileEntries = new List<Arc0ArchiveFileInfo>();
            foreach (var entry in _entries)
            {
                var fileInfo = castedFiles.First(c => c.Entry.fileOffset == entry.fileOffset);
                fileEntries.Add(fileInfo);

                // Catch file limits
                if (fileInfo.FileSize > 0xffffffff)
                {
                    throw new InvalidOperationException($"File '{fileInfo.FilePath}' is too big to pack into this archive type!");
                }

                if (offset + dataOffset > 0xffffffff)
                {
                    throw new InvalidOperationException("The archive can't be bigger than 0xFFFFFFFF Bytes.");
                }

                // Update entry
                entry.fileOffset = offset;
                entry.fileSize = (uint)fileInfo.FileSize;

                //write entry
                bw.WriteType(entry);

                //edit values
                offset = (uint)(((offset + fileInfo.FileSize) + 0x4) & ~0x4);
            }

            // Write name table
            _nameComp.Position = 0;
            _nameComp.CopyTo(output);

            // Write files
            bw.BaseStream.Position = dataOffset;
            foreach (var file in files)
                file.SaveFileData(output, null);

            // Write header
            _header.nameOffset = (uint)(0x48 + _table1.Length + _table2.Length + _entries.Count * 0x10 + 4);
            _header.dataOffset = (uint)dataOffset;

            bw.BaseStream.Position = 0;
            bw.WriteType(_header);
        }

        private IEnumerable<string> GetFileNames(Stream names)
        {
            using var br = new BinaryReaderX(names);

            var currentDir = "";
            br.BaseStream.Position = 1;

            while (br.BaseStream.Position < br.BaseStream.Length)
            {
                var tmpString = br.ReadCStringSJIS();
                if (tmpString[tmpString.Length - 1] == '/')
                    currentDir = tmpString;
                else
                    yield return currentDir + tmpString;
            }
        }

        // TODO: Remove when only net core
        private uint ToUInt32LittleEndian(byte[] input)
        {
#if NET_CORE_31
            return BinaryPrimitives.ReadUInt32LittleEndian(input);
#else
            return (uint)((input[3] << 24) | (input[2] << 16) | (input[1] << 8) | input[0]);
#endif
        }
    }
}
