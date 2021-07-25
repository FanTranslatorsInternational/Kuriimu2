using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Plugins.State;
using Kontract.Interfaces.Plugins.State.Archive;
using Kontract.Interfaces.Progress;
using Kontract.Interfaces.Providers;
using Kontract.Models.Archive;
using Kontract.Models.Context;
using Kontract.Models.IO;
using plugin_nintendo.Compression;

namespace plugin_nintendo.Archives
{
    class DarcState : IArchiveState, ILoadFiles, ISaveFiles, IReplaceFiles
    {
        private readonly Darc _arc;
        private NintendoCompressionMethod _method;

        public IList<IArchiveFileInfo> Files { get; private set; }

        public bool ContentChanged => IsChanged();

        public DarcState()
        {
            _arc = new Darc();
        }

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            if (TryDecompress(fileStream, out var decompressedFile, out _method))
                fileStream = decompressedFile;

            Files = _arc.Load(fileStream);
        }

        public Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            var output = _method == NintendoCompressionMethod.Unsupported ?
                fileSystem.OpenFile(savePath, FileMode.Create) :
                new MemoryStream();

            _arc.Save(output, Files);

            if (_method != NintendoCompressionMethod.Unsupported)
            {
                var final = fileSystem.OpenFile(savePath, FileMode.Create);

                output.Position = 0;
                NintendoCompressor.GetConfiguration(_method).Build().Compress(output, final);
            }

            return Task.CompletedTask;
        }

        public void ReplaceFile(IArchiveFileInfo afi, Stream fileData)
        {
            afi.SetFileData(fileData);
        }

        private bool IsChanged()
        {
            return Files.Any(x => x.ContentChanged);
        }

        private bool TryDecompress(Stream input, out Stream decompressedFile, out NintendoCompressionMethod method)
        {
            decompressedFile = null;

            method = NintendoCompressor.PeekCompressionMethod(input);
            if (method == NintendoCompressionMethod.Unsupported)
                return false;

            try
            {
                decompressedFile = new MemoryStream();
                NintendoCompressor.GetConfiguration(method).Build().Decompress(input, decompressedFile);
                decompressedFile.Position = 0;
            }
            catch
            {
                input.Position = 0;
                return false;
            }

            return true;
        }
    }
}
