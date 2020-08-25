using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Plugins.State;
using Kontract.Interfaces.Plugins.State.Archive;
using Kontract.Models.Archive;
using Kontract.Models.Context;
using Kontract.Models.IO;

namespace plugin_level5._3DS.Archives
{
    class Arc0State : IArchiveState, ILoadFiles, ISaveFiles, IReplaceFiles, IRenameFiles, IRemoveFiles, IAddFiles
    {
        private readonly Arc0 _arc0;
        private bool _hasDeletedFiles;
        private bool _hasAddedFiles;

        public IList<ArchiveFileInfo> Files { get; private set; }
        public bool ContentChanged => IsChanged();

        public Arc0State()
        {
            _arc0 = new Arc0();
        }

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            Files = _arc0.Load(fileStream);
        }

        public Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            var output = fileSystem.OpenFile(savePath, FileMode.Create);
            _arc0.Save(output, Files, saveContext.ProgressContext);

            _hasDeletedFiles = false;
            _hasAddedFiles = false;
            return Task.CompletedTask;
        }

        public void ReplaceFile(ArchiveFileInfo afi, Stream fileData)
        {
            afi.SetFileData(fileData);
        }

        private bool IsChanged()
        {
            return _hasDeletedFiles || _hasAddedFiles || Files.Any(x => x.ContentChanged);
        }

        public void Rename(ArchiveFileInfo afi, UPath path)
        {
            afi.FilePath = path;
        }

        public void RemoveFile(ArchiveFileInfo afi)
        {
            Files.Remove(afi);
            _hasDeletedFiles = true;
        }

        public void RemoveAll()
        {
            Files.Clear();
            _hasDeletedFiles = true;
        }

        public ArchiveFileInfo AddFile(Stream fileData, UPath filePath)
        {
            var newAfi = new Arc0ArchiveFileInfo(fileData, filePath.FullName, new Arc0FileEntry());
            Files.Add(newAfi);

            _hasAddedFiles = true;

            return newAfi;
        }
    }
}
