using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Kanvas;
using Komponent.IO;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Plugins.State;
using Kontract.Kanvas;
using Kontract.Models.Context;
using Kontract.Models.Image;
using Kontract.Models.IO;
using plugin_cattle_call.Compression;

namespace plugin_cattle_call.Images
{
    class F3xtState : IImageState, ILoadFiles, ISaveFiles
    {
        private F3xt _img;

        private bool _wasCompressed;
        private NintendoCompressionMethod _compMethod;

        public EncodingDefinition EncodingDefinition { get; }
        public IList<IKanvasImage> Images { get; private set; }

        public bool ContentChanged => IsContentChanged();

        public F3xtState()
        {
            _img = new F3xt();

            EncodingDefinition = F3xtSupport.GetEncodingDefinition();
        }

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            fileStream = Decompress(fileStream);

            Images = new List<IKanvasImage> { new KanvasImage(EncodingDefinition, _img.Load(fileStream)) };
        }

        public Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            var fileStream = _wasCompressed ? new MemoryStream() : fileSystem.OpenFile(savePath, FileMode.Create, FileAccess.Write);

            _img.Save(fileStream, Images[0].ImageInfo);

            if (_wasCompressed)
            {
                var compStream = fileSystem.OpenFile(savePath, FileMode.Create, FileAccess.Write);

                fileStream.Position = 0;
                NintendoCompressor.Compress(fileStream, compStream, _compMethod);
            }

            return Task.CompletedTask;
        }

        private bool IsContentChanged()
        {
            return Images.Any(x => x.ImageInfo.ContentChanged);
        }

        private Stream Decompress(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            input.Position = 5;
            var magic = br.ReadString(4);

            if (magic != "F3XT")
            {
                _wasCompressed = false;
                return input;
            }

            _wasCompressed = true;

            var ms = new MemoryStream();
            input.Position = 0;

            _compMethod = NintendoCompressor.PeekCompressionMethod(input);
            NintendoCompressor.Decompress(input, ms);

            ms.Position = 0;
            return ms;
        }
    }
}
