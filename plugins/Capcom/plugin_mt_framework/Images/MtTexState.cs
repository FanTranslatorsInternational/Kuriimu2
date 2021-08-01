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

namespace plugin_mt_framework.Images
{
    class MtTexState : IImageState, ILoadFiles, ISaveFiles
    {
        private readonly MtTex _tex;

        public EncodingDefinition EncodingDefinition { get; private set; }
        public IList<IKanvasImage> Images { get; private set; }

        public bool ContentChanged => IsContentChanged();

        public MtTexState()
        {
            _tex = new MtTex();
        }

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);

            var platform = MtTexSupport.DeterminePlatform(fileStream, loadContext.DialogManager);
            EncodingDefinition = MtTexSupport.GetEncodingDefinition(platform);

            Images = _tex.Load(fileStream, platform).Select(x => new KanvasImage(EncodingDefinition, x, ShouldLock(platform, x))).ToArray();
        }

        public Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            var fileStream = fileSystem.OpenFile(savePath, FileMode.Create, FileAccess.Write);
            _tex.Save(fileStream, Images.Select(x => x.ImageInfo).ToArray());

            return Task.CompletedTask;
        }

        private bool IsContentChanged()
        {
            return Images.Any(x => x.ImageInfo.ContentChanged);
        }

        private bool ShouldLock(MtTexPlatform platform, ImageInfo info)
        {
            // Lock transcoding for mobile formats, since the 3 images are linked together
            return platform == MtTexPlatform.Mobile;
        }
    }
}
