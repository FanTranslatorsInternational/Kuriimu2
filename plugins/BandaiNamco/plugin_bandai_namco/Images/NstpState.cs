using Komponent.IO;
using Komponent.Streams;
using Kompression;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Plugin.File;
using Konnect.Contract.Plugin.File.Image;

namespace plugin_bandai_namco.Images
{
    class NstpState : ILoadFiles, ISaveFiles, IImageFilePluginState
    {
        private Nstp _img = new();
        private bool _isCompressed;
        private List<NstpImageFile> _images;

        public IReadOnlyList<IImageFile> Images => _images;

        public bool ContentChanged => IsContentChanged();

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);
            fileStream = Decompress(fileStream);

            _images = _img.Load(fileStream);
        }

        public async Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            if (_isCompressed)
            {
                var ms = new MemoryStream();
                _img.Save(ms, _images);

                Stream fileStream = await fileSystem.OpenFileAsync(savePath, FileMode.Create, FileAccess.Write);

                ms.Position = 0;
                Compress(ms, fileStream);
            }
            else
            {
                Stream fileStream = await fileSystem.OpenFileAsync(savePath, FileMode.Create, FileAccess.Write);
                _img.Save(fileStream, _images);
            }
        }

        private bool IsContentChanged()
        {
            return Images.Any(x => x.ImageInfo.ContentChanged);
        }

        private Stream Decompress(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            var comp1 = br.ReadUInt32();
            if ((comp1 & 0xFF) == 0x19)
            {
                _isCompressed = true;

                var decompSize = comp1 >> 8;
                if (decompSize == 0)
                    decompSize = br.ReadUInt32();

                var ms = new MemoryStream();
                Compressions.TaikoLz80.Build().Decompress(new SubStream(input, input.Position, input.Length - input.Position), ms);
                ms.Position = 0;

                return ms;
            }

            input.Position = 0;
            return input;
        }

        private void Compress(Stream input, Stream output)
        {
            using var bw = new BinaryWriterX(output);

            // Write compression header
            var comp1 = 0x00000019;
            if (input.Length <= 0xFFFFFF)
                comp1 |= (int)(input.Length << 8);
            bw.Write(comp1);

            if (input.Length > 0xFFFFFF)
                bw.Write((uint)input.Length);

            Compressions.TaikoLz80.Build().Compress(input, output);

            bw.WriteAlignment(0x40);
        }
    }
}
