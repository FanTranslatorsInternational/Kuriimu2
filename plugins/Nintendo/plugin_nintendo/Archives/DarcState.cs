using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Plugin.File;
using Konnect.Contract.Plugin.File.Archive;
using plugin_nintendo.Common.Compression;

namespace plugin_nintendo.Archives
{
    class DarcState : ILoadFiles, ISaveFiles, IReplaceFiles, IRenameFiles, IRemoveFiles, IAddFiles
    {
        private readonly Darc _arc = new();
        private NintendoCompressionMethod _method;

        private List<IArchiveFile> _files;
        private bool _hasDeletedFiles;

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

            _hasDeletedFiles = false;
        }

        public void ReplaceFile(IArchiveFile afi, Stream fileData)
        {
            afi.SetFileData(fileData);
        }

        public void RenameFile(IArchiveFile file, UPath path)
        {
            if (file is not DarcArchiveFile darcFile)
                return;

            darcFile.FilePath = path;
            darcFile.UnescapedPath = $".{path.FullName?.Replace('/', '\\')}";
        }

        public IArchiveFile AddFile(Stream fileData, UPath filePath)
        {
            var file = new DarcArchiveFile(new ArchiveFileInfo
            {
                FilePath = filePath,
                FileData = fileData,
                ContentChanged = true
            }, $".{filePath.FullName?.Replace('/', '\\')}");
            _files.Add(file);

            return file;
        }

        public void RemoveFile(IArchiveFile file)
        {
            _hasDeletedFiles = true;
            _files.Remove(file);
        }

        public void RemoveAll()
        {
            _hasDeletedFiles = true;
            _files.Clear();
        }

        private bool IsChanged()
        {
            return Files.Any(x => x.ContentChanged) || _hasDeletedFiles;
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
