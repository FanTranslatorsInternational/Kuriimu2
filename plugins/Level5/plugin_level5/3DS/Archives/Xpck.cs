using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Extensions;
using Kontract.Models.Archive;
using Kryptography.Hash.Crc;
using plugin_level5.Compression;

namespace plugin_level5._3DS.Archives
{
    // Game: Yo-kai Watch, Time Travelers, more Level5 games in general on 3DS
    public class Xpck
    {
        private static readonly int HeaderSize = Tools.MeasureType(typeof(XpckHeader));
        private static readonly int EntrySize = Tools.MeasureType(typeof(XpckFileInfo));

        private XpckHeader _header;
        private bool _allowZlib;    // ZLib is only supported for Switch and newer; older platforms may not implement ZLib

        public IList<IArchiveFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Header
            _header = br.ReadType<XpckHeader>();

            // Entries
            br.BaseStream.Position = _header.FileInfoOffset;
            var entries = br.ReadMultiple<XpckFileInfo>(_header.FileCount);

            // File names
            var compNameTable = new SubStream(input, _header.FilenameTableOffset, _header.FilenameTableSize);
            _allowZlib = Level5Compressor.PeekCompressionMethod(compNameTable) == Level5CompressionMethod.ZLib;
            var decNames = new MemoryStream();
            Level5Compressor.Decompress(compNameTable, decNames);

            // Files
            using var nameList = new BinaryReaderX(decNames);

            var files = new List<IArchiveFileInfo>();
            foreach (var entry in entries)
            {
                nameList.BaseStream.Position = entry.nameOffset;
                var name = nameList.ReadCStringASCII();

                var fileData = new SubStream(input, _header.DataOffset + entry.FileOffset, entry.FileSize);
                if (name == "RES.bin")
                {
                    var method = Level5Compressor.PeekCompressionMethod(fileData);
                    var decompressedSize = Level5Compressor.PeekDecompressedSize(fileData);
                    var configuration = Level5Compressor.GetKompressionConfiguration(method);

                    files.Add(new XpckArchiveFileInfo(fileData, name, entry, configuration, decompressedSize));
                }
                else
                {
                    files.Add(new XpckArchiveFileInfo(fileData, name, entry));
                }
            }

            return files;
        }

        public void Save(Stream output, IList<IArchiveFileInfo> files)
        {
            var castedFiles = files.Cast<XpckArchiveFileInfo>().ToArray();
            using var bw = new BinaryWriterX(output);

            // Collect names
            var nameStream = new MemoryStream();
            using var nameBw = new BinaryWriterX(nameStream, true);
            foreach (var file in castedFiles)
            {
                file.FileEntry.nameOffset = (ushort)nameStream.Position;

                nameBw.WriteString(file.FilePath.ToRelative().FullName, Encoding.ASCII, false);
            }

            var nameStreamComp = new MemoryStream();
            Compress(nameStream, nameStreamComp, _allowZlib ? Level5CompressionMethod.ZLib : Level5CompressionMethod.Lz10);

            // Write files
            _header.DataOffset = (ushort)((HeaderSize + files.Count * EntrySize + nameStreamComp.Length + 3) & ~3);

            var crc32 = Crc32.Default;

            var fileOffset = (int)_header.DataOffset;
            foreach (var file in castedFiles.OrderBy(x => x.FileEntry.FileOffset))
            {
                output.Position = fileOffset;
                var writtenSize = file.SaveFileData(output);

                file.FileEntry.FileOffset = fileOffset - _header.DataOffset;
                file.FileEntry.FileSize = (int)writtenSize;
                file.FileEntry.hash = crc32.ComputeValue(file.FilePath.ToRelative().FullName);

                fileOffset = (int)output.Length;
            }

            _header.DataSize = (uint)output.Length - _header.DataOffset;

            // Entries
            _header.FileCount = (ushort)files.Count;
            _header.FileInfoOffset = (ushort)HeaderSize;
            _header.FileInfoSize = (ushort)(EntrySize * files.Count);

            bw.BaseStream.Position = HeaderSize;
            foreach (var file in castedFiles)
                bw.WriteType(file.FileEntry);

            // File names
            _header.FilenameTableOffset = (ushort)bw.BaseStream.Position;
            _header.FilenameTableSize = (ushort)((nameStreamComp.Length + 3) & ~3);
            nameStreamComp.CopyTo(output);

            // Header
            bw.BaseStream.Position = 0;
            bw.WriteType(_header);
        }

        private void Compress(Stream input, Stream output, Level5CompressionMethod compressionMethod)
        {
            input.Position = 0;
            output.Position = 0;

            Level5Compressor.Compress(input, output, compressionMethod);

            output.Position = 0;
            input.Position = 0;
        }
    }
}
