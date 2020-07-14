using System.Collections.Generic;
using System.Threading.Tasks;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Plugins.State;
using Kontract.Kanvas;
using Kontract.Models.Context;
using Kontract.Models.Image;
using Kontract.Models.IO;

namespace plugin_yuusha_shisu.BTX
{
    public class BtxState : IImageState, ILoadFiles
    {
        private readonly BTX _btx;

        public IList<ImageInfo> Images { get; private set; }
        public IDictionary<int, IColorEncoding> SupportedEncodings => BtxSupport.Encodings;
        public IDictionary<int, (IIndexEncoding, IList<int>)> SupportedIndexEncodings => BtxSupport.IndexEncodings;
        public IDictionary<int, IColorEncoding> SupportedPaletteEncodings => BtxSupport.PaletteEncodings;

        public BtxState()
        {
            _btx = new BTX();
        }

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            Images = new List<ImageInfo> { _btx.Load(fileStream) };
        }
    }
}
