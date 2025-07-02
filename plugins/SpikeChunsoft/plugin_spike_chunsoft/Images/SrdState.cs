using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Plugin.File;
using Konnect.Contract.Plugin.File.Image;
using Konnect.Extensions;
using Konnect.Plugin.File.Image;

namespace plugin_spike_chunsoft.Images
{
    class SrdState : ILoadFiles, ISaveFiles, IImageFilePluginState
    {
        private readonly Srd _img=new();
        private List<IImageFile> _images;

        public IReadOnlyList<IImageFile> Images => _images;

        public bool ContentChanged => IsContentChanged();

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

            _images = _img.Load(srdStream, srdvStream).Select(IImageFile (x) => new ImageFile(x, SrdSupport.GetEncodingDefinition())).ToList();
        }

        public async Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            Stream srdStream;
            Stream srdvStream;

            if (savePath.GetExtensionWithDot() == ".srd")
            {
                srdStream = await fileSystem.OpenFileAsync(savePath, FileMode.Create, FileAccess.Write);
                srdvStream = await fileSystem.OpenFileAsync(savePath.ChangeExtension(".srdv"), FileMode.Create, FileAccess.Write);
            }
            else
            {
                srdStream = await fileSystem.OpenFileAsync(savePath.ChangeExtension(".srd"), FileMode.Create, FileAccess.Write);
                srdvStream = await fileSystem.OpenFileAsync(savePath, FileMode.Create, FileAccess.Write);
            }

            _img.Save(srdStream, srdvStream, _images.Select(x => x.ImageInfo).ToArray());
        }

        private bool IsContentChanged()
        {
            return _images.Any(x => x.ImageInfo.ContentChanged);
        }
    }
}
