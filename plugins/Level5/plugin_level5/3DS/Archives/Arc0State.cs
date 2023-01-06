using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Plugins.State;
using Kontract.Interfaces.Plugins.State.Archive;
using Kontract.Interfaces.Plugins.State.Features;
using Kontract.Models.FileSystem;
using Kontract.Models.Plugins.State;

namespace plugin_level5._3DS.Archives
{
    class Arc0State : IArchiveState, ILoadFiles, ISaveFiles, IReplaceFiles, IRenameFiles, IRemoveFiles, IAddFiles
    {
        private readonly Arc0 _arc0;
        private List<IArchiveFileInfo> _files;
        private bool _hasDeletedFiles;
        private bool _hasAddedFiles;

        public IReadOnlyList<IArchiveFileInfo> Files { get; private set; }
        public bool ContentChanged => _hasDeletedFiles || _hasAddedFiles || Files.Any(x => x.ContentChanged);

        public Arc0State()
        {
            _arc0 = new Arc0();
        }

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            Files =_files= _arc0.Load(fileStream);
        }

        public Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            var output = fileSystem.OpenFile(savePath, FileMode.Create);
            _arc0.Save(output, _files, saveContext.ProgressContext);

            _hasDeletedFiles = false;
            _hasAddedFiles = false;
            return Task.CompletedTask;
        }

        public void ReplaceFile(IArchiveFileInfo afi, Stream fileData)
        {
            afi.SetFileData(fileData);
        }

        public void RenameFile(IArchiveFileInfo afi, UPath path)
        {
            afi.FilePath = path;
        }

        public void RemoveFile(IArchiveFileInfo afi)
        {
            _files.Remove(afi);
            _hasDeletedFiles = true;
        }

        public void RemoveAll()
        {
            _files.Clear();
            _hasDeletedFiles = true;
        }

        public IArchiveFileInfo AddFile(Stream fileData, UPath filePath)
        {
            var newAfi = new Arc0ArchiveFileInfo(fileData, filePath.FullName, new Arc0FileEntry());
            _files.Add(newAfi);

            _hasAddedFiles = true;

            return newAfi;
        }
    }
}
