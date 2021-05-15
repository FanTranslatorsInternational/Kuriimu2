using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Komponent.IO;
using Kontract.Extensions;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Plugins.State;
using Kontract.Interfaces.Plugins.State.Archive;
using Kontract.Models.Archive;
using Kontract.Models.Context;
using Kontract.Models.IO;

namespace plugin_bandai_namco.Archives
{
    class IdxState : IArchiveState, ILoadFiles, ISaveFiles, IReplaceFiles
    {
        private Idx _arc;

        public IList<IArchiveFileInfo> Files { get; private set; }
        public bool ContentChanged => IsContentChanged();

        public IdxState()
        {
            _arc = new Idx();
        }

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);

            var apkFilePaths = fileSystem.EnumerateFiles(filePath.GetDirectory(), "*.apk");
            var apkStreams = apkFilePaths.Select(x => fileSystem.OpenFile(x)).ToArray();

            Files = _arc.Load(fileStream, apkStreams);
        }

        public Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            var fileStream = fileSystem.OpenFile(savePath, FileMode.Create, FileAccess.Write);

            // Create new APK's where files are changed
            var apkStreams = new List<(UPath, Stream)>();
            foreach (var path in _arc.ApkPaths)
            {
                var isChanged = Files.Where(x => x.FilePath.IsInDirectory(path.ToAbsolute(), true)).Any(x => x.ContentChanged);
                if (isChanged)
                    apkStreams.Add((path, fileSystem.OpenFile(savePath.GetDirectory() / path.GetName(), FileMode.Create, FileAccess.Write)));
            }

            _arc.Save(fileStream, apkStreams, Files);

            return Task.CompletedTask;
        }

        private bool IsContentChanged()
        {
            return Files.Any(x => x.ContentChanged);
        }

        public void ReplaceFile(IArchiveFileInfo afi, Stream fileData)
        {
            afi.SetFileData(fileData);
        }
    }
}
