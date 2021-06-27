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

namespace plugin_spike_chunsoft.Images
{
    class SrdState : IImageState, ILoadFiles, ISaveFiles
    {
        private Srd _img;

        public EncodingDefinition EncodingDefinition { get; }
        public IList<IKanvasImage> Images { get; private set; }

        public bool ContentChanged => IsContentChanged();

        public SrdState()
        {
            _img = new Srd();

            EncodingDefinition = SrdSupport.GetEncodingDefinition();
        }

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            Stream srdStream;
            Stream srdvStream;

            if (filePath.GetExtensionWithDot() == ".srd")
            {
                srdStream = await fileSystem.OpenFileAsync(filePath);
                srdvStream = await fileSystem.OpenFileAsync(filePath.ChangeExtension(".srdv"));
            }
            else
            {
                srdStream = await fileSystem.OpenFileAsync(filePath.ChangeExtension(".srd"));
                srdvStream = await fileSystem.OpenFileAsync(filePath);
            }

            Images = _img.Load(srdStream, srdvStream).Select(x => new KanvasImage(EncodingDefinition, x)).ToArray();
        }

        public Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            Stream srdStream;
            Stream srdvStream;

            if (savePath.GetExtensionWithDot() == ".srd")
            {
                srdStream = fileSystem.OpenFile(savePath, FileMode.Create, FileAccess.Write);
                srdvStream = fileSystem.OpenFile(savePath.ChangeExtension(".srdv"), FileMode.Create, FileAccess.Write);
            }
            else
            {
                srdStream = fileSystem.OpenFile(savePath.ChangeExtension(".srd"), FileMode.Create, FileAccess.Write);
                srdvStream = fileSystem.OpenFile(savePath, FileMode.Create, FileAccess.Write);
            }

            _img.Save(srdStream, srdvStream, Images.Select(x => x.ImageInfo).ToArray());

            return Task.CompletedTask;
        }

        private bool IsContentChanged()
        {
            return Images.Any(x => x.ContentChanged);
        }
    }
}
