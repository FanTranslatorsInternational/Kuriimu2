using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Kanvas;
using Komponent.IO.Streams;
using Kontract.Extensions;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.State;
using Kontract.Kanvas;
using Kontract.Models;
using Kontract.Models.Context;
using Kontract.Models.Image;
using Kontract.Models.IO;
using plugin_level5.Compression;

namespace plugin_level5.Mobile.Images
{
    class ImgaState : IImageState, ILoadFiles, ISaveFiles
    {
        private readonly IPluginManager _pluginManager;

        private Imga _img;
        private ImgaKtx _imgKtx;
        private int _format;

        public EncodingDefinition EncodingDefinition { get; private set; }
        public IList<IKanvasImage> Images { get; private set; }

        public bool ContentChanged => IsContentChanged();

        public ImgaState(IPluginManager pluginManager)
        {
            _img = new Imga();
            _imgKtx = new ImgaKtx();
            _pluginManager = pluginManager;
        }

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);

            // Specially handle format 0x2B, which is KTX
            fileStream.Position = 0x0A;
            _format = fileStream.ReadByte();
            fileStream.Position = 0;

            switch (_format)
            {
                // Load KTX by plugin
                case 0x2B:
                    var imageInfo = _imgKtx.Load(fileStream, _pluginManager);

                    EncodingDefinition = _imgKtx.EncodingDefinition;
                    Images = new List<IKanvasImage> { new KanvasImage(EncodingDefinition, imageInfo) };
                    break;

                // Otherwise load normal IMGA
                default:
                    EncodingDefinition = ImgaSupport.GetEncodingDefinition();
                    Images = new List<IKanvasImage> { new KanvasImage(EncodingDefinition, _img.Load(fileStream)) };
                    break;
            }
        }

        public Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            var fileStream = fileSystem.OpenFile(savePath, FileMode.Create, FileAccess.Write);

            switch (_format)
            {
                // Save KTX by plugin
                case 0x2B:
                    _imgKtx.Save(fileStream, _pluginManager);
                    break;

                // Otherwise save normal IMGA
                default:
                    _img.Save(fileStream, Images[0]);
                    break;
            }

            return Task.CompletedTask;
        }

        private bool IsContentChanged()
        {
            return Images.Any(x => x.ImageInfo.ContentChanged);
        }
    }
}
