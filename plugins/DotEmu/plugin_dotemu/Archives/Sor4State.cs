using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Kontract.Extensions;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Plugins.State;
using Kontract.Interfaces.Plugins.State.Archive;
using Kontract.Models.Archive;
using Kontract.Models.Context;
using Kontract.Models.IO;

namespace plugin_dotemu.Archives
{
    class Sor4State : IArchiveState, ILoadFiles, ISaveFiles, IReplaceFiles
    {
        private Sor4 _arc;

        public IList<IArchiveFileInfo> Files { get; private set; }

        public bool ContentChanged => IsContentChanged();

        public Sor4State()
        {
            _arc = new Sor4();
        }

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

            Files = _arc.Load(texStream, texListStream, Sor4Support.DeterminePlatform(texListStream));
        }

        public Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            Stream texStream;
            Stream texListStream;
            if (savePath.GetName().StartsWith("textures"))
            {
                texStream = fileSystem.OpenFile(savePath, FileMode.Create, FileAccess.Write);
                texListStream = fileSystem.OpenFile(savePath.GetDirectory() / "texture_table" + savePath.GetName()[8..], FileMode.Create, FileAccess.Write);
            }
            else
            {
                texStream = fileSystem.OpenFile(savePath.GetDirectory() / "textures" + savePath.GetName()[13..], FileMode.Create, FileAccess.Write);
                texListStream = fileSystem.OpenFile(savePath, FileMode.Create, FileAccess.Write);
            }

            _arc.Save(texStream, texListStream, Files);

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
    }
}
