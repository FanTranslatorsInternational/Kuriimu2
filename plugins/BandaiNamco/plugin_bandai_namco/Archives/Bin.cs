using System.Buffers.Binary;
using Komponent.IO;
using Komponent.Streams;
using Kompression;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Plugin.File.Archive;

namespace plugin_bandai_namco.Archives
{
    class Bin
    {
        public List<IArchiveFile> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read data offset
            var dataOffset = br.ReadInt32();

            // Read offsets
            var offsets = new List<int> { dataOffset };
            while (input.Position < dataOffset)
            {
                var offset = br.ReadInt32();
                if (offset == 0)
                    break;

                offsets.Add(offset);
            }

            // Add files
            var result = new List<IArchiveFile>();
            for (var i = 0; i < offsets.Count - 1; i++)
            {
                var subStream = new SubStream(input, offsets[i], offsets[i + 1] - offsets[i]);
                var fileName = $"{i:00000000}{BinSupport.DetermineExtension(subStream)}";

                result.Add(CreateAfi(subStream, fileName));
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFile> files)
        {
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var fileOffset = ((files.Count + 1) * 4 + 0x7F) & ~0x7F;

            // Write files
            var filePosition = fileOffset;

            var offsets = new List<int>();
            foreach (var file in files)
            {
                output.Position = filePosition;
                var writtenSize = file.WriteFileData(output);

                offsets.Add(filePosition);

                filePosition = (filePosition + (int)writtenSize + 0x7F) & ~0x7F;
            }
            offsets.Add(filePosition);

            // Write offsets
            output.Position = 0;
            WriteIntegers(offsets, bw);
        }

        private IArchiveFile CreateAfi(Stream file, string fileName)
        {
            var buffer = new byte[4];

            file.Position = 0;
            _ = file.Read(buffer, 0, 4);

            if (buffer.SequenceEqual(new byte[] { 0x45, 0x43, 0x44, 0x01 }))
            {
                file.Position = 0xC;
                _ = file.Read(buffer, 0, 4);

                var decompressedSize = BinaryPrimitives.ReadInt32BigEndian(buffer);
                return new ArchiveFile(new CompressedArchiveFileInfo
                {
                    FilePath = fileName,
                    FileData = file,
                    Compression = Compressions.LzEcd.Build(),
                    DecompressedSize = decompressedSize
                });
            }

            return new ArchiveFile(new ArchiveFileInfo
            {
                FilePath = fileName,
                FileData = file
            });
        }

        private void WriteIntegers(IList<int> entries, BinaryWriterX writer)
        {
            foreach(int entry in entries)
                writer.Write(entry);
        }
    }
}
