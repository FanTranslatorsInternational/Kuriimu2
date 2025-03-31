using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.DataClasses.Plugin.File.Image;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Plugin.File;
using Konnect.Contract.Plugin.File.Image;
using Konnect.Plugin.File.Image;

namespace plugin_nintendo.Images
{
    public class CtpkState : IImageFilePluginState, ILoadFiles, ISaveFiles
    {
        private readonly Ctpk _ctpk = new();

        private List<ImageFileInfo> _imageInfos;

        public IReadOnlyList<IImageFile> Images { get; private set; }

        public bool ContentChanged => IsChanged();

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);
            _imageInfos = _ctpk.Load(fileStream);

            Images = _imageInfos.Select(x => new ImageFile(x, CtpkSupport.GetEncodingDefinitions())).ToArray();
        }

        public async Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            Stream saveStream = await fileSystem.OpenFileAsync(savePath, FileMode.Create, FileAccess.Write);
            _ctpk.Save(saveStream, _imageInfos);
        }

        private bool IsChanged()
        {
            return Images.Any(x => x.ImageInfo.ContentChanged);
        }
    }
}
