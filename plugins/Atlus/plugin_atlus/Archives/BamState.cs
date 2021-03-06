﻿using System;
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

namespace plugin_atlus.Archives
{
    class BamState : IArchiveState, ILoadFiles, ISaveFiles, IReplaceFiles
    {
        private Bam _bam;

        public IList<IArchiveFileInfo> Files { get; private set; }

        public bool ContentChanged => IsContentChanged();

        public BamState()
        {
            _bam = new Bam();
        }

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            Files = new List<IArchiveFileInfo> { _bam.Load(fileStream) };
        }

        public Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            var fileStream = fileSystem.OpenFile(savePath, FileMode.Create, FileAccess.Write);
            _bam.Save(fileStream, Files[0]);

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
