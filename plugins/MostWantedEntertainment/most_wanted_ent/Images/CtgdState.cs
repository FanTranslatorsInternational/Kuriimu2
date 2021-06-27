using System;
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
using most_wanted_ent.Compression;

namespace most_wanted_ent.Images
{
    class CtgdState : IImageState, ILoadFiles, ISaveFiles
    {
        private Ctgd _img;
        private bool _wasCompressed;

        public EncodingDefinition EncodingDefinition { get; }
        public IList<IKanvasImage> Images { get; private set; }

        public bool ContentChanged => IsContentChanged();

        public CtgdState()
        {
            _img = new Ctgd();

            EncodingDefinition = CtgdSupport.GetEncodingDefinition();
        }

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);

            // Decompress, if necessary
            using var br = new BinaryReaderX(fileStream, true);
            fileStream.Position = 9;
            if (br.ReadString(4) == "nns_")
            {
                _wasCompressed = true;

                fileStream.Position = 0;

                var decompressedStream = new MemoryStream();
                NintendoCompressor.Decompress(fileStream, decompressedStream);

                fileStream.Close();
                fileStream = decompressedStream;
            }

            fileStream.Position = 0;
            Images = new List<IKanvasImage> { new KanvasImage(EncodingDefinition, _img.Load(fileStream)) };
        }

        public Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            var fileStream = _wasCompressed ? new MemoryStream() : fileSystem.OpenFile(savePath, FileMode.Create, FileAccess.Write);
            _img.Save(fileStream, Images[0].ImageInfo);

            // Compress, if necessary
            if (_wasCompressed)
            {
                var compressedStream = fileSystem.OpenFile(savePath, FileMode.Create, FileAccess.Write);

                fileStream.Position = 0;
                NintendoCompressor.Compress(fileStream, compressedStream, NintendoCompressionMethod.Lz10);
            }

            return Task.CompletedTask;
        }

        private bool IsContentChanged()
        {
            return Images.Any(x => x.ImageInfo.ContentChanged);
        }
    }
}
