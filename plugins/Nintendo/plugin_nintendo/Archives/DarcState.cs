using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Plugin.File;
using Konnect.Contract.Plugin.File.Archive;
using plugin_nintendo.Common.Compression;

namespace plugin_nintendo.Archives
{
    class DarcState : ILoadFiles, ISaveFiles, IReplaceFiles
    {
        private readonly Darc _arc = new();
        private NintendoCompressionMethod _method;

        private List<IArchiveFile> _files;

        public IReadOnlyList<IArchiveFile> Files => _files;

        public bool ContentChanged => IsChanged();

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);
            if (TryDecompress(fileStream, out Stream decompressedFile, out _method))
                fileStream = decompressedFile;

            _files = _arc.Load(fileStream);
        }

        public async Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            Stream output = _method == NintendoCompressionMethod.Unsupported ?
                await fileSystem.OpenFileAsync(savePath, FileMode.Create, FileAccess.Write) :
                new MemoryStream();

            _arc.Save(output, _files);

            if (_method != NintendoCompressionMethod.Unsupported)
            {
                Stream final = await fileSystem.OpenFileAsync(savePath, FileMode.Create, FileAccess.Write);

                output.Position = 0;
                NintendoCompressor.Compress(output, final, _method);
            }
        }

        public void ReplaceFile(IArchiveFile afi, Stream fileData)
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
                NintendoCompressor.Decompress(input, decompressedFile);
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
