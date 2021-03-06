using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Kanvas;
using Kanvas.Encoding;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Plugins.State;
using Kontract.Kanvas;
using Kontract.Models.Context;
using Kontract.Models.Image;
using Kontract.Models.IO;

namespace plugin_nintendo.Images
{
    class BnrState : IImageState, ILoadFiles, ISaveFiles
    {
        private readonly Bnr _bnr;

        public EncodingDefinition EncodingDefinition { get; }
        public IList<IKanvasImage> Images { get; private set; }

        public bool ContentChanged => IsContentChanged();

        public BnrState()
        {
            _bnr = new Bnr();

            EncodingDefinition = new EncodingDefinition();
            EncodingDefinition.AddPaletteEncoding(0, ImageFormats.Rgb555());
            EncodingDefinition.AddIndexEncoding(0, ImageFormats.I4(BitOrder.LeastSignificantBitFirst), new[] { 0 });
        }

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            Images = new IKanvasImage[] { new KanvasImage(EncodingDefinition, _bnr.Load(fileStream)) };
        }

        public Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            var fileStream = fileSystem.OpenFile(savePath, FileMode.Create, FileAccess.ReadWrite);
            _bnr.Save(fileStream, Images[0].ImageInfo);

            return Task.CompletedTask;
        }

        private bool IsContentChanged()
        {
            return Images.Any(x => x.ImageInfo.ContentChanged);
        }
    }
}
