﻿using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models.Archive;
using Kontract.Models.Context;
using Kontract.Models.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace plugin_square_enix.Archives
{
    public class BinState : IArchiveState, ILoadFiles, ISaveFiles
    {
        private readonly Bin _bin;
        
        public IList<IArchiveFileInfo> Files { get; private set; }
        
        public bool ContentChanged { get; private set; }
        
        public BinState()
        {
            _bin = new Bin();
        }

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext context)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            Files = _bin.Load(fileStream);
        }

        public Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            var output = fileSystem.OpenFile(savePath, FileMode.Create);
            _bin.Save(output, Files);

            return Task.CompletedTask;
        }

    }

}