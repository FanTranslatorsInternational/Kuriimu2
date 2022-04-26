using Kanvas;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Plugins.State;
using Kontract.Kanvas;
using Kontract.Models.Context;
using Kontract.Models.Image;
using Kontract.Models.IO;
using plugin_dotemu.Images;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace plugin_capcom.Images
{
    class SdtState : IImageState, ILoadFiles
    {
        private Sdt _img;

        public EncodingDefinition EncodingDefinition { get; }
        public IList<IKanvasImage> Images { get; private set; }

        public bool ContentChanged => IsContentChanged();

        public SdtState()
        {
            _img = new Sdt();

            EncodingDefinition = SdtSupport.GetEncodingDefinition();
        }

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);
            Images = new List<IKanvasImage> { new KanvasImage(EncodingDefinition, _img.Load(fileStream)) };
        }

        private bool IsContentChanged()
        {
            return Images.Any(x => x.ContentChanged);
        }
    }
}
