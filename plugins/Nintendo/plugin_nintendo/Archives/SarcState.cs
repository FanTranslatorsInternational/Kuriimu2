using System.Buffers.Binary;
using Komponent.Contract.Enums;
using Komponent.IO;
using Komponent.Streams;
using Kompression;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Plugin.File;
using Konnect.Contract.Plugin.File.Archive;

namespace plugin_nintendo.Archives
{
    class SarcState : ILoadFiles, ISaveFiles, IReplaceFiles
    {
        private readonly Sarc _arc = new();

        private string _compMagic;
        private List<IArchiveFile> _files;

        public IReadOnlyList<IArchiveFile> Files => _files;
        public bool ContentChanged => IsContentChanged();

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);

            // Decompress, if necessary
            fileStream = Decompress(fileStream);

            _files = _arc.Load(fileStream);
        }

        public async Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            var fileStream = _compMagic == null ?
                await fileSystem.OpenFileAsync(savePath, FileMode.Create, FileAccess.Write) :
                new MemoryStream();

            _arc.Save(fileStream, _files, _compMagic != null);

            // Compress if necessary
            if (_compMagic != null)
            {
                fileStream.Position = 0;
                var compStream = await fileSystem.OpenFileAsync(savePath, FileMode.Create, FileAccess.Write);

                Compress(fileStream, compStream);
            }
        }

        public void ReplaceFile(IArchiveFile afi, Stream fileData)
        {
            afi.SetFileData(fileData);
        }

        private bool IsContentChanged()
        {
            return Files.Any(x => x.ContentChanged);
        }

        private Stream Decompress(Stream input)
        {
            using var br = new BinaryReaderX(input, true, ByteOrder.BigEndian);
            br.BaseStream.Position += 4;

            // Detect ZLib
            var magicByte = br.ReadByte();
            if ((magicByte & 0xF) == 8 && (magicByte & 0xF0) <= 0x70)
            {
                _compMagic = "zlib";

                var destination = new MemoryStream();
                Compressions.ZLib.Build().Decompress(new SubStream(input, 4, input.Length - 4), destination);
                destination.Position = 0;

                return destination;
            }

            // Detect Yaz0
            br.BaseStream.Position = 0;
            _compMagic = br.PeekString(4);

            if (_compMagic == "Yaz0")
            {
                var decompStream = new MemoryStream();

                input.Position = 0;
                Compressions.Nintendo.Yaz0Be.Build().Decompress(input, decompStream);

                decompStream.Position = 0;
                return decompStream;
            }

            // Default to no compression
            _compMagic = null;

            input.Position = 0;
            return input;
        }

        private void Compress(Stream input, Stream output)
        {
            switch (_compMagic)
            {
                case "Yaz0":
                    Compressions.Nintendo.Yaz0Be.Build().Compress(input, output);
                    break;

                case "zlib":
                    var decompSizeBytes = new byte[4];

                    BinaryPrimitives.WriteInt32BigEndian(decompSizeBytes, (int)input.Length);
                    output.Write(decompSizeBytes);

                    Compressions.ZLib.Build().Compress(input, output);
                    break;
            }
        }
    }
}
