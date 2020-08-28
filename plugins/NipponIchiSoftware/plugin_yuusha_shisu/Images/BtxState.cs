using System.Collections.Generic;
using System.Threading.Tasks;
using Kanvas;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Plugins.State;
using Kontract.Kanvas;
using Kontract.Models.Context;
using Kontract.Models.Image;
using Kontract.Models.IO;

namespace plugin_yuusha_shisu.Images
{
    public class BtxState : IImageState, ILoadFiles
    {
        private readonly BTX _btx;

        public EncodingDefinition EncodingDefinition { get; }
        public IList<IKanvasImage> Images { get; private set; }

        public BtxState()
        {
            _btx = new BTX();

            EncodingDefinition = BtxSupport.Encodings.ToColorDefinition();
            EncodingDefinition.AddPaletteEncodings(BtxSupport.PaletteEncodings);
            EncodingDefinition.AddIndexEncodings(BtxSupport.IndexEncodings);
        }

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            var img = _btx.Load(fileStream);

            Images = new List<IKanvasImage> { new KanvasImage(EncodingDefinition, img) };
        }
    }
}
