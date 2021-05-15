using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Kanvas;
using Kontract.Extensions;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Plugins.State;
using Kontract.Kanvas;
using Kontract.Models.Context;
using Kontract.Models.Image;
using Kontract.Models.IO;

namespace plugin_level5.DS.Images
{
    class GtxtState : IImageState, ILoadFiles, ISaveFiles
    {
        private Gtxt _img;

        public EncodingDefinition EncodingDefinition { get; }
        public IList<IKanvasImage> Images { get; private set; }

        public bool ContentChanged => IsContentChanged();

        public GtxtState()
        {
            _img = new Gtxt();

            EncodingDefinition = GtxtSupport.GetEncodingDefinition();
        }

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            Stream ltFileStream;
            Stream lpFileStream;

            if (filePath.GetExtensionWithDot() == ".lt")
            {
                ltFileStream = await fileSystem.OpenFileAsync(filePath);
                lpFileStream = await fileSystem.OpenFileAsync(filePath.ChangeExtension(".lp"));
            }
            else
            {
                ltFileStream = await fileSystem.OpenFileAsync(filePath.ChangeExtension(".lt"));
                lpFileStream = await fileSystem.OpenFileAsync(filePath);
            }

            Images = new List<IKanvasImage> { new KanvasImage(EncodingDefinition, _img.Load(ltFileStream, lpFileStream)) };
        }

        public Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            Stream ltFileStream;
            Stream lpFileStream;

            if (savePath.GetExtensionWithDot() == ".lt")
            {
                ltFileStream = fileSystem.OpenFile(savePath, FileMode.Create, FileAccess.Write);
                lpFileStream = fileSystem.OpenFile(savePath.ChangeExtension(".lp"), FileMode.Create, FileAccess.Write);
            }
            else
            {
                ltFileStream = fileSystem.OpenFile(savePath.ChangeExtension(".lt"), FileMode.Create, FileAccess.Write);
                lpFileStream = fileSystem.OpenFile(savePath, FileMode.Create, FileAccess.Write);
            }

            _img.Save(ltFileStream, lpFileStream, Images[0].ImageInfo);

            return Task.CompletedTask;
        }

        private bool IsContentChanged()
        {
            return Images.Any(x => x.ImageInfo.ContentChanged);
        }
    }
}
