using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models.Archive;
using Kontract.Models.Context;
using Kontract.Models.IO;

namespace plugin_sony.Archives.PSARC
{
    /// <summary>
    /// 
    /// </summary>
    class PsarcState : IArchiveState, ILoadFiles
    {
        /// <summary>
        /// 
        /// </summary>
        private readonly PSARC _psarc;

        /// <summary>
        /// 
        /// </summary>
        public IList<IArchiveFileInfo> Files { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public bool ContentChanged => throw new NotImplementedException();

        /// <summary>
        /// 
        /// </summary>
        public PsarcState()
        {
            _psarc = new PSARC();
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
            Files = _psarc.Load(fileStream);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileSystem"></param>
        /// <param name="savePath"></param>
        /// <param name="saveContext"></param>
        /// <returns></returns>
        public async Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            throw new NotImplementedException();
        }
    }
}
