using System.Collections.Generic;
using System.Threading.Tasks;
using Kanvas;
using Kanvas.Encoding;
using Komponent.IO.Attributes;
using Kontract.Extensions;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Plugins.State;
using Kontract.Kanvas;
using Kontract.Models.Context;
using Kontract.Models.Image;
using Kontract.Models.IO;

namespace plugin_mcdonalds.Images
{
    class ECDPNcgrState : IImageState, ILoadFiles
    {
        private ECDPNcgr _ncgr;

        public EncodingDefinition EncodingDefinition { get; }
        public IList<IKanvasImage> Images { get; private set; }

        public ECDPNcgrState()
        {
            _ncgr = new ECDPNcgr();

            EncodingDefinition = new EncodingDefinition();
            EncodingDefinition.AddPaletteEncoding(0, new Rgba(5, 5, 5, "BGR"));
            EncodingDefinition.AddIndexEncoding(3, new Index(4), new[] { 0 });
            EncodingDefinition.AddIndexEncoding(4, new Index(8), new[] { 0 });
        }

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            var ncgrStream = await fileSystem.OpenFileAsync(filePath);
            var nclrStream = await fileSystem.OpenFileAsync(filePath.ChangeExtension("NCLR"));

            var kanvasImage = new KanvasImage(EncodingDefinition, _ncgr.Load(ncgrStream, nclrStream));

            Images = new List<IKanvasImage> { kanvasImage };
        }
    }
}
