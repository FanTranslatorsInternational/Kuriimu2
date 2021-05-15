using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Komponent.IO;
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
            _compMagic = br.ReadString(4);

            var decompStream = new MemoryStream();
            switch (_compMagic)
            {
                case "Yaz0":
                    input.Position = 0;
                    Compressions.Nintendo.Yaz0Be.Build().Decompress(input, decompStream);

                    decompStream.Position = 0;
                    return decompStream;
            }

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
            }
        }
    }
}
