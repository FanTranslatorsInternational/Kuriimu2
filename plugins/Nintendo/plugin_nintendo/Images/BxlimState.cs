using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.DataClasses.Plugin.File.Image;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Plugin.File;
using Konnect.Contract.Plugin.File.Image;
using Konnect.Plugin.File.Image;

namespace plugin_nintendo.Images
{
    public class BxlimState : IImageFilePluginState, ILoadFiles, ISaveFiles
    {
        private readonly Bxlim _bxlim = new();

        private ImageFileInfo _imageInfo;

        public IReadOnlyList<IImageFile> Images { get; private set; }

        public bool ContentChanged => IsContentChanged();

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);
            _imageInfo = _bxlim.Load(fileStream);

            var encodingDefinition = _bxlim.IsCtr ? BxlimSupport.GetCtrDefinition() : BxlimSupport.GetCafeDefinition();

            Images = [new ImageFile(_imageInfo, encodingDefinition)];
        }

        public async Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            Stream saveStream = await fileSystem.OpenFileAsync(savePath, FileMode.Create, FileAccess.Write);
            _bxlim.Save(saveStream, _imageInfo);
        }

        private bool IsContentChanged()
        {
            return Images.Any(x => x.ImageInfo.ContentChanged);
        }
    }
}
