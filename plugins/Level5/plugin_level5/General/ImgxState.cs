using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Kanvas;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Managers.Files;
using Kontract.Interfaces.Plugins.State;
using Kontract.Interfaces.Plugins.State.Features;
using Kontract.Kanvas.Interfaces;
using Kontract.Models.FileSystem;
using Kontract.Models.Plugins.State;

namespace plugin_level5.General
{
    public class ImgxState : IImageState, ILoadFiles, ISaveFiles
    {
        private readonly IFileManager _fileManager;

        private Imgx _img;
        private ImgxKtx _ktx;
        private int _format;
        private List<IImageInfo> _images;

        public IReadOnlyList<IImageInfo> Images { get; private set; }

        public bool ContentChanged => _images.Any(x => x.ContentChanged);

        public ImgxState(IFileManager fileManager)
        {
            _img = new Imgx();
            _ktx = new ImgxKtx();
            _fileManager = fileManager;
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
                    var imageInfo = _ktx.Load(fileStream, _fileManager);

                    Images = new List<IImageInfo> { imageInfo };
                    break;

                // Otherwise load normal IMGx
                default:
                    var data = _img.Load(fileStream);
                    var def = await ImgxSupport.GetEncodingDefinition(_img.Magic, _img.Format, _img.BitDepth, loadContext.DialogManager);

                    Images = _images = new List<IImageInfo> { new KanvasImageInfo(def, data) };
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
                    _ktx.Save(fileStream, _fileManager);
                    break;

                // Otherwise save normal IMGx
                default:
                    _img.Save(fileStream, _images[0]);
                    break;
            }

            return Task.CompletedTask;
        }
    }
}
