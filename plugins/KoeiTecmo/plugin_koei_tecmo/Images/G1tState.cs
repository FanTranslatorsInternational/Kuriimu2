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

namespace plugin_koei_tecmo.Images
{
    class G1tState : IImageState, ILoadFiles, ISaveFiles
    {
        private G1t _img;

        public EncodingDefinition EncodingDefinition { get; private set; }
        public IList<IKanvasImage> Images { get; private set; }

        public bool ContentChanged => IsContentChanged();

        public G1tState()
        {
            _img = new G1t();
        }

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);

            var platform = G1tSupport.DeterminePlatform(fileStream, loadContext.DialogManager);
            EncodingDefinition = G1tSupport.GetEncodingDefinition(platform);

            fileStream.Position = 0;
            Images = _img.Load(fileStream, platform).Select(x => new KanvasImage(EncodingDefinition, x)).ToArray();
        }

        public Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            var fileStream = fileSystem.OpenFile(savePath, FileMode.Create, FileAccess.Write);
            _img.Save(fileStream, Images.Select(x => x.ImageInfo).ToArray());

            return Task.CompletedTask;
        }

        private bool IsContentChanged()
        {
            return Images.Any(x => x.ImageInfo.ContentChanged);
        }
    }
}
