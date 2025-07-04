using Komponent.IO;
using Komponent.Streams;
using Kompression;
using Kompression.Decoder.Headerless;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Archive;

namespace plugin_shade.Archives
{
    // Game: Inazuma Eleven GO Strikers 2013
    // HINT: Despite being on Wii, this archive is Little Endian
    // HINT: Unbelievably ugly archive. Ignore everything that's done here and move on with your life, god dammit
    class BlnSub
    {
        public List<IArchiveFile> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read files
            var result = new List<IArchiveFile>();

            var index = 0;
            while (br.BaseStream.Position < input.Length)
            {
                var sample = br.ReadInt32();
                if (sample == 0x7FFF)
                    break;

                br.BaseStream.Position -= 4;
                var entry = ReadEntry(br);

                if (entry.size == 0)
                    break;

                var stream = new SubStream(input, br.BaseStream.Position, entry.size);
                result.Add(CreateAfi(stream, index++, entry));

                br.BaseStream.Position += entry.size;
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFile> files)
        {
            // Write files
            using var bw = new BinaryWriterX(output);
            foreach (var file in files.Cast<BlnSubArchiveFile>())
            {
                var startOffset = output.Position;
                output.Position += 0xC;

                var writtenSize = file.WriteFileData(output, true);

                var endOffset = startOffset + writtenSize + 0xC;
                output.Position = startOffset;
                WriteEntry(file.Entry, bw);

                output.Position = endOffset;
            }

            // Write end entry
            bw.Write(0x7FFF);
            bw.WriteAlignment(0x1000);
        }

        private IArchiveFile CreateAfi(Stream stream, int index, BlnSubEntry entry)
        {
            // Every file not compressed with the headered Spike Chunsoft compression, is compressed headerless
            var compressionMagic = ShadeSupport.PeekInt32LittleEndian(stream);
            if (compressionMagic != 0xa755aafc)
                return new BlnSubArchiveFile(new CompressedArchiveFileInfo
                {
                    FilePath = ShadeSupport.CreateFileName(index, stream, false),
                    FileData = stream,
                    Compression = Compressions.ShadeLzHeaderless.Build(),
                    DecompressedSize = (int)ShadeLzHeaderlessDecoder.CalculateDecompressedSize(stream)
                }, entry);

            stream.Position = 0;
            return new BlnSubArchiveFile(new CompressedArchiveFileInfo
            {
                FilePath = ShadeSupport.CreateFileName(index, stream, true),
                FileData = stream,
                Compression = Compressions.ShadeLz.Build(),
                DecompressedSize = (int)ShadeSupport.PeekDecompressedSize(stream)
            }, entry);
        }

        private BlnSubEntry ReadEntry(BinaryReaderX reader)
        {
            return new BlnSubEntry
            {
                archiveIndex = reader.ReadInt32(),
                archiveOffset = reader.ReadInt32(),
                size = reader.ReadInt32()
            };
        }

        private void WriteEntry(BlnSubEntry entry, BinaryWriterX writer)
        {
            writer.Write(entry.archiveIndex);
            writer.Write(entry.archiveOffset);
            writer.Write(entry.size);
        }
    }
}
