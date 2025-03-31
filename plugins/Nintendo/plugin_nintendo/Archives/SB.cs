using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Plugin.File.Archive;

namespace plugin_nintendo.Archives
{
    public class SB
    {
        private const int HeaderSize_ = 0x4;

        public List<IArchiveFile> Load(Stream input)
        {
            var typeReader = new BinaryTypeReader();
            using var br = new BinaryReaderX(input, true);

            // Read header
            var header = typeReader.Read<SbHeader>(br);

            // Read offsets
            var offsets = typeReader.ReadMany<uint>(br, header.entryCount);

            // Add files
            var result = new List<IArchiveFile>();
            for (var i = 0; i < offsets.Count; i++)
            {
                var endOffset = i + 1 < offsets.Count ? offsets[i + 1] : input.Length;
                var fileStream = new SubStream(input, offsets[i], endOffset - offsets[i]);

                result.Add(new ArchiveFile(new ArchiveFileInfo
                {
                    FilePath = $"{i:00000000}.bin",
                    FileData = fileStream
                }));
            }

            return result;
        }

        public void Save(Stream output, List<IArchiveFile> files)
        {
            var dataPosition = (HeaderSize_ + (files.Count + 1) * 4 + 0x7F) & ~0x7F;

            var typeWriter = new BinaryTypeWriter();
            using var bw = new BinaryWriterX(output);

            // Write files
            bw.BaseStream.Position = dataPosition;

            var offsets = new List<uint>();
            foreach (var file in files)
            {
                offsets.Add((uint)bw.BaseStream.Position);

                file.WriteFileData(bw.BaseStream);
                bw.WriteAlignment(0x80);
            }

            // Write offsets
            bw.BaseStream.Position = HeaderSize_;
            typeWriter.WriteMany(offsets, bw);
            bw.Write(bw.BaseStream.Length);

            // Write header
            bw.BaseStream.Position = 0;
            typeWriter.Write(new SbHeader
            {
                entryCount = (short)files.Count
            }, bw);
        }
    }
}
