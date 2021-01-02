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

namespace plugin_level5._3DS.Images
{
    public class ImgcState : IImageState, ILoadFiles, ISaveFiles
    {
        private Imgc imgc;

        public EncodingDefinition EncodingDefinition { get; private set; }
        public IList<IKanvasImage> Images { get; private set; }

        public bool ContentChanged => IsChanged();

        public ImgcState()
        {
            imgc = new Imgc();
        }

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            var img = imgc.Load(fileStream);

            EncodingDefinition = ImgcSupport.DetermineFormatMapping(img.ImageFormat, imgc.BitDepth, loadContext.DialogManager);
            Images = new List<IKanvasImage> { new KanvasImage(EncodingDefinition, img) };
        }

        public Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            var fileStream = fileSystem.OpenFile(savePath, FileMode.Create);
            imgc.Save(fileStream, Images[0]);

            return Task.CompletedTask;
        }

        private bool IsChanged()
        {
            return Images.Any(x => x.ImageInfo.ContentChanged);
        }
    }
}
