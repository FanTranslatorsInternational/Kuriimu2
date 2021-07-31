using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using Komponent.IO;
using Komponent.IO.Streams;
using Kompression.Implementations;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Plugins.State;
using Kontract.Interfaces.Plugins.State.Archive;
using Kontract.Models.Archive;
using Kontract.Models.Context;
using Kontract.Models.IO;

namespace plugin_nintendo.Archives
{
    class SarcState : IArchiveState, ILoadFiles, ISaveFiles, IReplaceFiles
    {
        private Sarc _arc;

        private string _compMagic;

        public IList<IArchiveFileInfo> Files { get; private set; }
        public bool ContentChanged => IsContentChanged();

        public SarcState()
        {
            _arc = new Sarc();
        }

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);

            // Decompress, if necessary
            fileStream = Decompress(fileStream);

            Files = _arc.Load(fileStream);
        }

        public Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            var fileStream = _compMagic == null ?
                fileSystem.OpenFile(savePath, FileMode.Create, FileAccess.Write) :
                new MemoryStream();

            _arc.Save(fileStream, Files, _compMagic != null);

            // Compress if necessary
            if (_compMagic != null)
            {
                fileStream.Position = 0;
                var compStream = fileSystem.OpenFile(savePath, FileMode.Create, FileAccess.Write);

                Compress(fileStream, compStream);
            }

            return Task.CompletedTask;
        }

        public void ReplaceFile(IArchiveFileInfo afi, Stream fileData)
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

                var destination=new MemoryStream();
                Compressions.ZLib.Build().Decompress(new SubStream(input, 4, input.Length - 4),destination);
                destination.Position = 0;

                return destination;
            }

            // Detect Yaz0
            br.BaseStream.Position = 0;
            _compMagic = br.PeekString();

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

                    BinaryPrimitives.WriteInt32BigEndian(decompSizeBytes,(int)input.Length);
                    output.Write(decompSizeBytes);

                    Compressions.ZLib.Build().Compress(input, output);
                    break;
            }
        }
    }
}
