using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Kanvas;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Plugins.State;
using Kontract.Kanvas;
using Kontract.Models.Context;
using Kontract.Models.Image;
using Kontract.Models.IO;
using plugin_nintendo.Compression;

namespace plugin_nintendo.Images
{
    class RawJtexState : IImageState, ILoadFiles, ISaveFiles
    {
        private RawJtex _raw;
        private NintendoCompressionMethod? _method;

        public EncodingDefinition EncodingDefinition { get; }
        public IList<IKanvasImage> Images { get; private set; }

        public bool ContentChanged => IsContentChanged();

        public RawJtexState()
        {
            _raw = new RawJtex();

            EncodingDefinition = RawJtexSupport.GetEncodingDefinition();
        }

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            if (IsCompressed(fileStream))
                _method = NintendoCompressor.PeekCompressionMethod(fileStream);

            if (_method != null)
                fileStream = Decompress(fileStream);

            Images = new List<IKanvasImage> { new KanvasImage(EncodingDefinition, _raw.Load(fileStream)) };
        }

        public Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            var fileStream = _method != null ?
                new MemoryStream() :
                fileSystem.OpenFile(savePath, FileMode.Create, FileAccess.Write);

            _raw.Save(fileStream, Images[0].ImageInfo);

            if (_method == null)
                return Task.CompletedTask;

            var output = fileSystem.OpenFile(savePath, FileMode.Create, FileAccess.Write);

            fileStream.Position = 0;
            NintendoCompressor.Compress(fileStream, output, _method.Value);

            return Task.CompletedTask;
        }

        private bool IsContentChanged()
        {
            return Images.Any(x => x.ImageInfo.ContentChanged);
        }

        private bool IsCompressed(Stream input)
        {
            var buffer = new byte[4];
            input.Read(buffer);

            input.Position = 0;
            return (buffer[0] == 0x10 || buffer[0] == 0x11) && (buffer[1] != 0 || buffer[2] != 0 || buffer[3] != 0);
        }

        private Stream Decompress(Stream input)
        {
            var output = new MemoryStream();
            NintendoCompressor.Decompress(input, output);

            output.Position = 0;
            return output;
        }
    }
}
