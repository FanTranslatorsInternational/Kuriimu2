using Komponent.IO;
using Komponent.Streams;
using Kompression;
using Kompression.Decoder.Headerless;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Archive;

namespace plugin_shade.Archives
{
    class Bin
    {
        private BinHeader _header;

        public List<IArchiveFile> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read Header
            _header = ReadHeader(br);

            // Read entries
            var entries = ReadInfos(br, _header.fileCount);

            // Read files
            var files = new List<IArchiveFile>();
            for (var i = 0; i < entries.Length; i++)
            {
                var entry = entries[i];
                var offset = (entry.offSize >> _header.shiftFactor) * _header.padFactor;
                var size = (entry.offSize & _header.mask) * _header.mulFactor;

                var stream = new SubStream(input, offset, size);
                files.Add(CreateAfi(stream, i, entry));
            }

            return files;
        }

        public void Save(Stream output, IList<IArchiveFile> files)
        {
            using var bw = new BinaryWriterX(output);
            var castedFiles = files.Cast<BinArchiveFile>().ToArray();

            // Write files
            foreach (var file in castedFiles)
            {
                var offset = (file.Entry.offSize >> _header.shiftFactor) * _header.padFactor;
                output.Position = offset;

                file.WriteFileData(output);
            }
            bw.WriteAlignment(_header.padFactor);

            // Write header
            output.Position = 0;
            WriteHeader(_header, bw);

            // Write entries
            foreach (var file in castedFiles)
                bw.Write(file.Entry.offSize);
        }

        private IArchiveFile CreateAfi(Stream stream, int index, BinFileInfo entry)
        {
            // Every file not compressed with the headered Spike Chunsoft compression, is compressed headerless
            var compressionMagic = ShadeSupport.PeekInt32LittleEndian(stream);
            if (compressionMagic != 0xa755aafc)
                return new BinArchiveFile(new CompressedArchiveFileInfo
                {
                    FilePath = ShadeSupport.CreateFileName(index, stream, false),
                    FileData = stream,
                    Compression = Compressions.ShadeLzHeaderless.Build(),
                    DecompressedSize = (int)ShadeLzHeaderlessDecoder.CalculateDecompressedSize(stream)
                }, entry);

            stream.Position = 0;
            return new BinArchiveFile(new CompressedArchiveFileInfo
            {
                FilePath = ShadeSupport.CreateFileName(index, stream, true),
                FileData = stream,
                Compression = Compressions.ShadeLz.Build(),
                DecompressedSize = (int)ShadeSupport.PeekDecompressedSize(stream)
            }, entry);
        }

        private BinHeader ReadHeader(BinaryReaderX reader)
        {
            return new BinHeader
            {
                fileCount = reader.ReadInt32(),
                padFactor = reader.ReadInt32(),
                mulFactor = reader.ReadInt32(),
                shiftFactor = reader.ReadInt32(),
                mask = reader.ReadInt32()
            };
        }

        private BinFileInfo[] ReadInfos(BinaryReaderX reader, int count)
        {
            var result = new BinFileInfo[count];

            for (var i = 0; i < count; i++)
                result[i] = ReadInfo(reader);

            return result;
        }

        private BinFileInfo ReadInfo(BinaryReaderX reader)
        {
            return new BinFileInfo
            {
                offSize = reader.ReadUInt32()
            };
        }

        private void WriteHeader(BinHeader header, BinaryWriterX writer)
        {
            writer.Write(header.fileCount);
            writer.Write(header.padFactor);
            writer.Write(header.mulFactor);
            writer.Write(header.shiftFactor);
            writer.Write(header.mask);
        }
    }
}