using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.DataClasses.Plugin.File.Image;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Plugin.File;
using Konnect.Contract.Plugin.File.Image;
using Konnect.Extensions;
using Konnect.Plugin.File.Image;
using SixLabors.ImageSharp;

namespace plugin_nintendo.Images
{
    class Tex0State : IImageFilePluginState, ILoadFiles, ISaveFiles
    {
        private readonly Tex0 _img = new();

        private ImageFileInfo _imageInfo;

        public IReadOnlyList<IImageFile> Images { get; private set; }

        public bool ContentChanged => IsContentChanged();

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            var texPath = filePath;
            var pltPath = $"{filePath.GetDirectory()}/../Palettes(NW4R)/{filePath.GetName()}";

            var texStream = await fileSystem.OpenFileAsync(texPath);
            var pltStream = fileSystem.FileExists(pltPath) ? await fileSystem.OpenFileAsync(pltPath) : null;

            _imageInfo = _img.Load(texStream, pltStream);

            Images = [new ImageFile(_imageInfo, Tex0Support.GetEncodingDefinition())];
        }

        public Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            var texPath = savePath;
            var pltPath = $"{savePath.GetDirectory()}/../Palettes(NW4R)/{savePath.GetName()}";

            var texStream = fileSystem.OpenFile(texPath, FileMode.Create, FileAccess.Write);
            var pltStream = _imageInfo.PaletteData is not null ? fileSystem.OpenFile(pltPath, FileMode.Create, FileAccess.Write) : null;

            _img.Save(texStream, pltStream, _imageInfo);

            return Task.CompletedTask;
        }

        private bool IsContentChanged()
        {
            return Images.Any(x => x.ImageInfo.ContentChanged);
        }
    }
}
