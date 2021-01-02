using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Kanvas;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Plugins.State;
using Kontract.Kanvas;
using Kontract.Models.Context;
using Kontract.Models.Image;
using Kontract.Models.IO;
using plugin_nintendo.NW4C;

namespace plugin_nintendo.Images
{
    public class BclimState : IImageState, ILoadFiles, ISaveFiles
    {
        private readonly Bclim _bclim;

        public EncodingDefinition EncodingDefinition { get; }

        public IList<IKanvasImage> Images { get; private set; }

        public bool ContentChanged => IsChanged();

        public BclimState()
        {
            _bclim = new Bclim();

            EncodingDefinition = ImageFormats.CtrFormats.ToColorDefinition();
        }

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            var img = _bclim.Load(fileStream);

            Images = new List<IKanvasImage> { new KanvasImage(EncodingDefinition, img) };
        }

        public Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            var saveStream = fileSystem.OpenFile(savePath, FileMode.Create);
            _bclim.Save(saveStream, Images[0]);

            return Task.CompletedTask;
        }

        private bool IsChanged()
        {
            return Images.Any(x => x.ImageInfo.ContentChanged);
        }
    }
}
