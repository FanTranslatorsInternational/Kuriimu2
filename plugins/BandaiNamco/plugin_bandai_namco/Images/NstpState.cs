using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Kanvas;
using Komponent.IO;
using Komponent.IO.Streams;
using Kompression.Implementations;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Plugins.State;
using Kontract.Kanvas;
using Kontract.Models.Context;
using Kontract.Models.Image;
using Kontract.Models.IO;

namespace plugin_bandai_namco.Images
{
    class NstpState : IImageState, ILoadFiles, ISaveFiles
    {
        private Nstp _img;
        private bool _isCompressed;

        public EncodingDefinition EncodingDefinition { get; }
        public IList<IKanvasImage> Images { get; private set; }

        public bool ContentChanged => IsContentChanged();

        public NstpState()
        {
            _img = new Nstp();

            EncodingDefinition = NstpSupport.GetEncodingDefinition();
        }

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            fileStream = Decompress(fileStream);

            Images = _img.Load(fileStream).Select(x => new KanvasImage(EncodingDefinition, x)).ToArray();
        }

        public Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            var fileStream = fileSystem.OpenFile(savePath, FileMode.Create, FileAccess.Write);
            _img.Save(fileStream, Images.Select(x => x.ImageInfo).ToArray());

            if (!_isCompressed)
                return Task.CompletedTask;

            // Compress file
            fileStream = fileSystem.OpenFile(savePath);
            var compFile = fileSystem.OpenFile(savePath + ".comp", FileMode.Create, FileAccess.Write);
            Compress(fileStream, compFile);

            fileStream.Close();
            compFile.Close();

            // Set compressed file as saved file
            fileSystem.DeleteFile(savePath);
            fileSystem.MoveFile(savePath + ".comp", savePath);

            return Task.CompletedTask;
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
