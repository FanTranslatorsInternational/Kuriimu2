using System.Collections.Generic;
using System.IO;
using System.Linq;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Models.Archive;

namespace plugin_koei_tecmo.Archives
{
    // TODO: Test plugin
    // Game: Yo-Kai Watch: Sangoukushi
    class X3
    {
        private static readonly int HeaderSize = Tools.MeasureType(typeof(X3Header));
        private static readonly int EntrySize = Tools.MeasureType(typeof(X3FileEntry));

        private X3Header _header;

        public IList<IArchiveFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            _header = br.ReadType<X3Header>();
            br.BaseStream.Position += 4;

            // Read file entries
            var entries = br.ReadMultiple<X3FileEntry>(_header.fileCount);

            var firstBlocksList = new List<(int, int, string)>();

            // Add files
            var result = new List<IArchiveFileInfo>();
            foreach (var entry in entries)
            {
                var fileOffset = entry.offset * _header.offsetMultiplier;
                br.BaseStream.Position = fileOffset;

                var firstBlockLength = -1;
                Stream firstBlock;
                if (entry.IsCompressed)
                {
                    // Compressed files have decompressed size and size of the first "block" prefixed
                    br.BaseStream.Position += 4;
                    firstBlockLength = br.ReadInt32();
                    firstBlock = PeekFirstCompressedBlock(input, input.Position, firstBlockLength);
                }
                else
                {
                    // Uncompressed files have only uncompressed size prefixed
                    firstBlock = new SubStream(input, br.BaseStream.Position, 4);
                }

                var extension = DetermineExtension(firstBlock);

                var fileStream = new SubStream(br.BaseStream, fileOffset + (entry.IsCompressed ? 8 : 0), entry.fileSize);
                var fileName = result.Count.ToString("00000000") + extension;

                if (firstBlockLength >= 0)
                    firstBlocksList.Add((firstBlockLength, entry.fileSize, fileName));

                //if (fileName == "00000001.3ds.gt1")
                //{
                //    var newFs = File.Create(@"D:\Users\Kirito\Desktop\comp_x3.bin");
                //    fileStream.CopyTo(newFs);
                //    fileStream.Position = 0;
                //    newFs.Close();
                //}

                if (entry.IsCompressed)
                    result.Add(new X3ArchiveFileInfo(fileStream, fileName,
                        Kompression.Implementations.Compressions.ZLib, entry.decompressedFileSize,
                        entry, firstBlockLength));
                else
                    result.Add(new X3ArchiveFileInfo(fileStream, fileName,
                        entry, firstBlockLength));
            }

            return result;
        }

        // TODO: Set firstBlockLength again (need to understand enough ZLib for that)
        public void Save(Stream output, IList<IArchiveFileInfo> files)
        {
            using var bw = new BinaryWriterX(output);
            var castedFiles = files.Cast<X3ArchiveFileInfo>().ToArray();

            var header = new X3Header
            {
                fileCount = files.Count
            };

            // Write files
            bw.BaseStream.Position = (HeaderSize + 4 + files.Count * EntrySize + header.offsetMultiplier - 1) & ~(header.offsetMultiplier - 1);

            foreach (var file in castedFiles)
            {
                var fileOffset = bw.BaseStream.Position;

                if (file.Entry.IsCompressed)
                {
                    // Write prefix information when compressed
                    bw.Write((uint)file.FileSize);
                    bw.Write(file.FirstBlockSize);
                }

                var writtenSize = file.SaveFileData(bw.BaseStream);
                bw.WriteAlignment(header.offsetMultiplier);

                file.Entry.offset = fileOffset / header.offsetMultiplier;
                file.Entry.fileSize = (int)writtenSize;
                if (file.Entry.IsCompressed)
                    file.Entry.decompressedFileSize = (int)file.FileSize;
            }

            // Write file entries
            bw.BaseStream.Position = HeaderSize + 4;
            foreach (var file in castedFiles)
                bw.WriteType(file.Entry);

            // Write header
            bw.BaseStream.Position = 0;
            bw.WriteType(header);
        }

        private Stream PeekFirstCompressedBlock(Stream input, long offset, long firstBlockSize)
        {
            var subStream = new SubStream(input, offset, firstBlockSize);
            var ms = new MemoryStream();

            Kompression.Implementations.Compressions.ZLib.Build().Decompress(subStream, ms);

            ms.Position = 0;
            return ms;
        }

        private string DetermineExtension(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            switch (br.ReadString(4))
            {
                case "GT1G":
                    return ".3ds.gt1";

                case "SMDH":
                    return ".icn";

                default:
                    return ".bin";
            }
        }
    }
}
