using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using Komponent.IO;
using Kontract.Models.Archive;
using plugin_level5.Compression;
using System.Linq;
using System.Text;
using Komponent.IO.Streams;
using Kontract.Extensions;
using Kryptography.Hash.Crc;
using plugin_level5._3DS.Archives;

namespace plugin_level5.DS.Archives
{
    class Gfsa
    {
        private static int HeaderSize = Tools.MeasureType(typeof(GfsaHeader));
        private static int DirectoryEntrySize = Tools.MeasureType(typeof(GfsaDirectoryEntry));
        private static int FileEntrySize = Tools.MeasureType(typeof(GfsaFileEntry));

        private GfsaHeader _header;
        private byte[] _unkTable;

        public IList<IArchiveFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            _header = br.ReadType<GfsaHeader>();

            // Read tables
            var directoryEntries = XfsaSupport.ReadCompressedTableEntries<GfsaDirectoryEntry>(input,
                _header.directoryOffset, _header.fileOffset - _header.directoryOffset,
                _header.directoryCount);

            var fileEntries = XfsaSupport.ReadCompressedTableEntries<GfsaFileEntry>(input,
                _header.fileOffset, _header.unkOffset - _header.fileOffset,
                _header.fileCount);

            input.Position = _header.unkOffset;
            _unkTable = br.ReadBytes(_header.stringOffset - _header.unkOffset);

            // Read strings
            var nameComp = new SubStream(input, _header.stringOffset, _header.fileDataOffset - _header.stringOffset);
            var nameStream = new MemoryStream();
            Level5Compressor.Decompress(nameComp, nameStream);
            var (directories, files) = ReadStrings(nameStream);

            // Add files
            var result = new List<IArchiveFileInfo>();
            foreach (var dirEntry in directoryEntries)
            {
                var dirName = directories.FirstOrDefault(x => x.Hash == dirEntry.hash)?.Value;

                for (var fileIndex = dirEntry.fileIndex; fileIndex < dirEntry.fileIndex + dirEntry.fileCount; fileIndex++)
                {
                    var fileEntry = fileEntries[fileIndex];
                    var fileName = files.Skip(dirEntry.fileIndex).Take(dirEntry.fileCount).FirstOrDefault(x => x.Hash == fileEntry.hash)?.Value;

                    var fileData = new SubStream(input, _header.fileDataOffset + fileEntry.Offset, fileEntry.Size);
                    result.Add(CreateAfi(fileData, Path.Combine(dirName, fileName), fileEntry));
                }
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFileInfo> files)
        {
            output.Position = HeaderSize;

            // Write directory table
            _header.directoryOffset = HeaderSize;
            _header.directoryCount = files.Select(x => x.FilePath.GetDirectory()).Distinct().Count();
            WriteDirectoryTable(output, files);

            // Write file entries
            _header.fileOffset = (int)output.Position;
            _header.fileCount = files.Count;
            WriteFileTable(output, files);

            // Write unknown table
            _header.unkOffset = (int)output.Position;
            output.Write(_unkTable);

            // Write strings
            _header.stringOffset = (int)output.Position;
            WriteStrings(output, files);

            // Write files
            _header.fileDataOffset = (int)output.Position;
            WriteFiles(output, files);

            // Write header
            _header.decompressedTableSize = _header.directoryCount * DirectoryEntrySize + _header.fileCount * FileEntrySize;
            output.Position = 0;
            using var bw = new BinaryWriterX(output);
            bw.WriteType(_header);
        }

        #region Load

        private (GfsaString[], GfsaString[]) ReadStrings(Stream stringStream)
        {
            var directories = new List<string>();
            var files = new List<string>();

            stringStream.Position = 0;

            using var br = new BinaryReaderX(stringStream);
            while (br.BaseStream.Position < br.BaseStream.Length)
            {
                var value = br.ReadCStringASCII();
                if (value.EndsWith('/'))
                    directories.Add(value);
                else
                    files.Add(value);
            }

            return (GetGfsaStrings(directories), GetGfsaStrings(files));
        }

        private GfsaString[] GetGfsaStrings(IList<string> values)
        {
            var crc16 = Crc16.X25;
            return values
                .Select(x => new GfsaString(x, BinaryPrimitives.ReadUInt16BigEndian(crc16.Compute(Encoding.ASCII.GetBytes(x)))))
                .ToArray();
        }

        private IArchiveFileInfo CreateAfi(Stream file, string name, GfsaFileEntry entry)
        {
            var method = Level5Compressor.PeekCompressionMethod(file);
            if (method != Level5CompressionMethod.NoCompression)
                return new GfsaArchiveFileInfo(file, name, entry, Level5Compressor.GetKompressionConfiguration(method), Level5Compressor.PeekDecompressedSize(file));

            return new GfsaArchiveFileInfo(new SubStream(file, 4, file.Length - 4), name, entry);
        }

        #endregion

        #region Save

        private void WriteDirectoryTable(Stream output, IList<IArchiveFileInfo> files)
        {
            var crc16 = Crc16.X25;
            var directoryEntries = new List<GfsaDirectoryEntry>();

            var fileIndex = 0;
            foreach (var group in OrderFiles(files).GroupBy(x => x.FilePath.GetDirectory().ToRelative() + "/"))
            {
                directoryEntries.Add(new GfsaDirectoryEntry
                {
                    hash = BinaryPrimitives.ReadUInt16BigEndian(crc16.Compute(Encoding.ASCII.GetBytes(group.Key))),
                    fileIndex = fileIndex,
                    fileCount = (short)group.Count()
                });

                fileIndex += directoryEntries.Last().fileCount;
            }

            XfsaSupport.WriteCompressedTableEntries(output, directoryEntries.OrderBy(x => x.hash));

            while (output.Position % 4 > 0) output.WriteByte(0);
        }

        private void WriteFileTable(Stream output, IList<IArchiveFileInfo> files)
        {
            var crc16 = Crc16.X25;
            var fileEntries = new List<GfsaFileEntry>();

            var fileOffset = 0;
            foreach (var fileGroup in OrderFiles(files).Cast<GfsaArchiveFileInfo>().GroupBy(x => x.FilePath.GetDirectory().ToRelative() + "/"))
            {
                var localGroup = fileGroup.Select(file =>
                {
                    var entry = new GfsaFileEntry
                    {
                        hash = BinaryPrimitives.ReadUInt16BigEndian(crc16.Compute(Encoding.ASCII.GetBytes(file.FilePath.GetName()))),
                        Offset = fileOffset,
                        Size = (int)file.CompressedSize
                    };

                    fileOffset += (int)((file.CompressedSize + 3) & ~3);

                    return entry;
                });

                fileEntries.AddRange(localGroup.OrderBy(x => x.hash));
            }

            XfsaSupport.WriteCompressedTableEntries(output, fileEntries);

            while (output.Position % 4 > 0) output.WriteByte(0);
        }

        private void WriteStrings(Stream output, IList<IArchiveFileInfo> afis)
        {
            var directories = new HashSet<string>();
            var files = new List<string>(afis.Count);

            foreach (var file in OrderFiles(afis))
            {
                directories.Add(file.FilePath.GetDirectory().ToRelative().FullName + "/");
                files.Add(file.FilePath.GetName());
            }

            var strings = new MemoryStream();
            using var bw = new BinaryWriterX(strings, true);
            foreach (var s in directories) bw.WriteString(s, Encoding.ASCII, false);
            foreach (var s in files) bw.WriteString(s, Encoding.ASCII, false);

            var compStrings = new MemoryStream();
            XfsaSupport.Compress(strings, compStrings, Level5CompressionMethod.Lz10);

            compStrings.CopyTo(output);

            while (output.Position % 4 > 0) output.WriteByte(0);
        }

        private void WriteFiles(Stream output, IList<IArchiveFileInfo> files)
        {
            foreach (GfsaArchiveFileInfo file in OrderFiles(files))
                file.SaveFileData(output);
        }

        private IEnumerable<IArchiveFileInfo> OrderFiles(IList<IArchiveFileInfo> files)
        {
            return files.GroupBy(x => x.FilePath.GetDirectory()).SelectMany(x => x.OrderBy(y => y.FilePath.GetName()));
        }

        #endregion
    }
}
