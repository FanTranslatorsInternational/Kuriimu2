using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.DataClasses.Plugin.File.Image;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Plugin.File;
using Konnect.Contract.Plugin.File.Image;
using Konnect.Extensions;
using Konnect.Plugin.File.Image;

namespace plugin_nintendo.Images
{
    class NcgrState : IImageFilePluginState, ILoadFiles
    {
        private readonly Ncgr _ncgr = new();

        private ImageFileInfo _imageInfo;

        public IReadOnlyList<IImageFile> Images { get; private set; }

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            var ncgrStream = await fileSystem.OpenFileAsync(filePath);
            var nclrStream = await fileSystem.OpenFileAsync(filePath.ChangeExtension("NCLR"));

            _imageInfo = _ncgr.Load(ncgrStream, nclrStream);

            Images = [new ImageFile(_imageInfo, NcgrSupport.GetEncodingDefinition())];
        }
    }
}
