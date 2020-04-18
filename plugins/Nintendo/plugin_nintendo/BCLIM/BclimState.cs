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
using plugin_nintendo.NW4C;

namespace plugin_nintendo.BCLIM
{
    public class BclimState : IImageState, ILoadFiles, ISaveFiles
    {
        private readonly Bclim _bclim;

        public IList<ImageInfo> Images { get; private set; }
        public IDictionary<int, IColorEncoding> SupportedEncodings => ImageFormats.CtrFormats;
        public IDictionary<int, (IIndexEncoding, IList<int>)> SupportedIndexEncodings { get; }
        public IDictionary<int, IColorEncoding> SupportedPaletteEncodings { get; }

        public bool ContentChanged { get; set; }

        public BclimState()
        {
            _bclim = new Bclim();
        }

        public async Task Load(IFileSystem fileSystem, UPath filePath, ITemporaryStreamProvider temporaryStreamProvider,
            IProgressContext progress)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            Images = new List<ImageInfo> { _bclim.Load(fileStream) };
        }

        public Task Save(IFileSystem fileSystem, UPath savePath, IProgressContext progress)
        {
            var saveStream = fileSystem.OpenFile(savePath, FileMode.Create);
            _bclim.Save(saveStream, Images[0]);

            return Task.CompletedTask;
        }
    }
}
