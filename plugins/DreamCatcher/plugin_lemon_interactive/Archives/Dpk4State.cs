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

namespace plugin_lemon_interactive.Archives
{
    /// <summary>
    /// 
    /// </summary>
    class Dpk4State : IArchiveState, ILoadFiles, ISaveFiles, IReplaceFiles
    {
        /// <summary>
        /// 
        /// </summary>
        private Dpk4 _dpk4;

        /// <summary>
        /// 
        /// </summary>
        public IList<IArchiveFileInfo> Files { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public bool ContentChanged => IsContentChanged();

        /// <summary>
        /// 
        /// </summary>
        public Dpk4State()
        {
            _dpk4 = new Dpk4();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileSystem"></param>
        /// <param name="filePath"></param>
        /// <param name="loadContext"></param>
        /// <returns></returns>
        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            Files = _dpk4.Load(fileStream);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileSystem"></param>
        /// <param name="savePath"></param>
        /// <param name="saveContext"></param>
        /// <returns></returns>
        public Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            var fileStream = fileSystem.OpenFile(savePath, FileMode.Create, FileAccess.Write);
            _dpk4.Save(fileStream, Files);

            return Task.CompletedTask;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="afi"></param>
        /// <param name="fileData"></param>
        public void ReplaceFile(IArchiveFileInfo afi, Stream fileData)
        {
            afi.SetFileData(fileData);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private bool IsContentChanged()
        {
            return true; //Files.Any(x => x.ContentChanged);
        }
    }
}
