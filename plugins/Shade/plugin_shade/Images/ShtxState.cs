using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using Kanvas;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Plugins.State;
using Kontract.Kanvas;
using Kontract.Models.Context;
using Kontract.Models.Image;
using Kontract.Models.IO;


namespace plugin_shade.Images
{
    public class ShtxState : IImageState, ILoadFiles, ISaveFiles
    {
        private readonly SHTX _shtx;
        public EncodingDefinition EncodingDefinition { get; private set; }
        public IList<IKanvasImage> Images { get; private set; }
        public bool ContentChanged => IsChanged();
        public ShtxState()
        {
            _shtx = new SHTX();

        }

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            EncodingDefinition = ShtxSupport.DetermineFormatMapping(loadContext.DialogManager);

            var fileStream = await fileSystem.OpenFileAsync(filePath);
            var img = _shtx.Load(fileStream);

            Images = new List<IKanvasImage> { new KanvasImage(EncodingDefinition, img) };
        }

        public Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            var fileStream = fileSystem.OpenFile(savePath, FileMode.Create);
            _shtx.Save(fileStream, Images[0].ImageInfo);

            return Task.CompletedTask;
        }

        private bool IsChanged()
        {
            return Images.Any(x => x.ImageInfo.ContentChanged);
        }
    }
}