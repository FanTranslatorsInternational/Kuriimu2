using Kanvas.Encoding;
using Komponent.Contract.Enums;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Plugin.File;
using Konnect.Contract.Plugin.File.Image;
using Konnect.Extensions;
using Konnect.Plugin.File.Image;

namespace plugin_mcdonalds.Images
{
    class ECDPNcgrState : IImageFilePluginState, ILoadFiles
    {
        private readonly ECDPNcgr _ncgr = new();
        private readonly EncodingDefinition _encodingDefintion;

        public IReadOnlyList<IImageFile> Images { get; private set; }

        public ECDPNcgrState()
        {
            _encodingDefintion = new EncodingDefinition();
            _encodingDefintion.AddPaletteEncoding(0, new Rgba(5, 5, 5, "BGR"));
            _encodingDefintion.AddIndexEncoding(3, new Kanvas.Encoding.Index(4, bitOrder: BitOrder.LeastSignificantBitFirst), [0]);
            _encodingDefintion.AddIndexEncoding(4, new Kanvas.Encoding.Index(8), [0]);
        }

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            Stream ncgrStream = await fileSystem.OpenFileAsync(filePath);
            Stream nclrStream = await fileSystem.OpenFileAsync(filePath.ChangeExtension("NCLR"));

            var kanvasImage = new ImageFile(_ncgr.Load(ncgrStream, nclrStream), _encodingDefintion);

            Images = [kanvasImage];
        }
    }
}
