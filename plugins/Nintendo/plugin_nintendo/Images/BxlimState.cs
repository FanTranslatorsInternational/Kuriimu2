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

namespace plugin_nintendo.Images
{
    public class BxlimState : IImageState, ILoadFiles, ISaveFiles
    {
        private readonly Bxlim _bxlim;

        public EncodingDefinition EncodingDefinition { get; private set; }

        public IList<IKanvasImage> Images { get; private set; }

        public bool ContentChanged => IsContentChanged();

        public BxlimState()
        {
            _bxlim = new Bxlim();
        }

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            var img = _bxlim.Load(fileStream);

            EncodingDefinition = _bxlim.IsCtr ? BxlimSupport.GetCtrDefinition() : BxlimSupport.GetCafeDefinition();

            Images = new List<IKanvasImage> { new KanvasImage(EncodingDefinition, img) };
        }

        public Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            var saveStream = fileSystem.OpenFile(savePath, FileMode.Create);
            _bxlim.Save(saveStream, Images[0].ImageInfo);

            return Task.CompletedTask;
        }

        private bool IsContentChanged()
        {
            return Images.Any(x => x.ImageInfo.ContentChanged);
        }
    }
}
