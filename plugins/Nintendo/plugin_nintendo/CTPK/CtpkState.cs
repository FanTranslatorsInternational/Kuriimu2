using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Plugins.State;
using Kontract.Interfaces.Progress;
using Kontract.Interfaces.Providers;
using Kontract.Kanvas;
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

        public bool ContentChanged { get; set; }

        public CtpkState()
        {
            _ctpk = new Ctpk();
        }

        public async Task Load(IFileSystem fileSystem, UPath filePath, ITemporaryStreamProvider temporaryStreamProvider,
            IProgressContext progress)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            Images = _ctpk.Load(fileStream);
        }

        public Task Save(IFileSystem fileSystem, UPath savePath, IProgressContext progress)
        {
            var saveStream = fileSystem.OpenFile(savePath, FileMode.Create);
            _ctpk.Save(Images, saveStream);

            return Task.CompletedTask;
        }
    }
}
