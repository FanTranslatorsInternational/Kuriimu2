using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Plugins.State;
using Kontract.Interfaces.Progress;
using Kontract.Interfaces.Providers;
using Kontract.Kanvas;
using Kontract.Models.Context;
using Kontract.Models.Image;
using Kontract.Models.IO;

namespace plugin_nintendo.CTPK
{
    public class CtpkState : IImageState, ILoadFiles, ISaveFiles
    {
        private readonly Ctpk _ctpk;

        public IList<ImageInfo> Images { get; private set; }
        public IDictionary<int, IColorEncoding> SupportedEncodings => Support.CtrFormat;
        public IDictionary<int, (IIndexEncoding, IList<int>)> SupportedIndexEncodings { get; }
        public IDictionary<int, IColorEncoding> SupportedPaletteEncodings { get; }

        public bool ContentChanged => IsChanged();

        public CtpkState()
        {
            _ctpk = new Ctpk();
        }

        public async void Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            Images = _ctpk.Load(fileStream);
        }

        public void Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            var saveStream = fileSystem.OpenFile(savePath, FileMode.Create);
            _ctpk.Save(Images, saveStream);
        }

        private bool IsChanged()
        {
            return Images.Any(x => x.ContentChanged);
        }
    }
}
