using System.Collections.Generic;
using System.Threading.Tasks;
using Kanvas;
using Kanvas.Encoding;
using Kontract.Extensions;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Plugins.State;
using Kontract.Kanvas;
using Kontract.Models.Context;
using Kontract.Models.Image;
using Kontract.Models.IO;

namespace plugin_nintendo.Images
{
    class NcgrState : IImageState, ILoadFiles
    {
        private Ncgr _ncgr;

        public EncodingDefinition EncodingDefinition { get; }

        public IList<IKanvasImage> Images { get; private set; }

        public NcgrState()
        {
            _ncgr = new Ncgr();

            EncodingDefinition = new EncodingDefinition();
            EncodingDefinition.AddPaletteEncoding(0, new Rgba(5, 5, 5, "BGR"));
            EncodingDefinition.AddIndexEncoding(3, ImageFormats.I4(BitOrder.LeastSignificantBitFirst), new[] { 0 });
            EncodingDefinition.AddIndexEncoding(4, ImageFormats.I8(), new[] { 0 });
        }

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            var ncgrStream = await fileSystem.OpenFileAsync(filePath);
            var nclrStream = await fileSystem.OpenFileAsync(filePath.ChangeExtension("NCLR"));

            var kanvasImage = new KanvasImage(EncodingDefinition, _ncgr.Load(ncgrStream, nclrStream));

            Images = new IKanvasImage[] { kanvasImage };
        }
    }
}
