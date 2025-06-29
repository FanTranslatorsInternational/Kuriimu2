using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Plugin.File;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Extensions;

namespace plugin_dotemu.Archives
{
    class Sor4State : ILoadFiles, ISaveFiles, IReplaceFiles
    {
        private readonly Sor4 _arc = new();
        private List<IArchiveFile> _files;

        public IReadOnlyList<IArchiveFile> Files => _files;

        public bool ContentChanged => IsContentChanged();

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            Stream texStream;
            Stream texListStream;
            if (filePath.GetName().StartsWith("textures"))
            {
                texStream = await fileSystem.OpenFileAsync(filePath);
                texListStream = await fileSystem.OpenFileAsync(filePath.GetDirectory() / "texture_table" + filePath.GetName()[8..]);
            }
            else
            {
                texStream = await fileSystem.OpenFileAsync(filePath.GetDirectory() / "textures" + filePath.GetName()[13..]);
                texListStream = await fileSystem.OpenFileAsync(filePath);
            }

            _files = _arc.Load(texStream, texListStream, Sor4Support.DeterminePlatform(texListStream));
        }

        public async Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            Stream texStream;
            Stream texListStream;
            if (savePath.GetName().StartsWith("textures"))
            {
                texStream = await fileSystem.OpenFileAsync(savePath, FileMode.Create, FileAccess.Write);
                texListStream = await fileSystem.OpenFileAsync(savePath.GetDirectory() / "texture_table" + savePath.GetName()[8..], FileMode.Create, FileAccess.Write);
            }
            else
            {
                texStream = await fileSystem.OpenFileAsync(savePath.GetDirectory() / "textures" + savePath.GetName()[13..], FileMode.Create, FileAccess.Write);
                texListStream = await fileSystem.OpenFileAsync(savePath, FileMode.Create, FileAccess.Write);
            }

            _arc.Save(texStream, texListStream, _files);
        }

        public void ReplaceFile(IArchiveFile afi, Stream fileData)
        {
            afi.SetFileData(fileData);
        }

        private bool IsContentChanged()
        {
            return _files.Any(x => x.ContentChanged);
        }
    }
}
