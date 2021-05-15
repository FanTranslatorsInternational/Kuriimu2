using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Kanvas;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.State;
using Kontract.Kanvas;
using Kontract.Models.Context;
using Kontract.Models.Image;
using Kontract.Models.IO;

namespace plugin_level5.General
{
    public class ImgxState : IImageState, ILoadFiles, ISaveFiles
    {
        private readonly IFileManager _fileManager;

        private Imgx _img;
        private ImgxKtx _ktx;
        private int _format;

        public EncodingDefinition EncodingDefinition { get; private set; }
        public IList<IKanvasImage> Images { get; private set; }

        public bool ContentChanged => IsContentChanged();

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

                    EncodingDefinition = _ktx.EncodingDefinition;
                    Images = new List<IKanvasImage> { new KanvasImage(EncodingDefinition, imageInfo) };
                    break;

                // Otherwise load normal IMGx
                default:
                    var loadedImg = _img.Load(fileStream);

                    EncodingDefinition = ImgxSupport.GetEncodingDefinition(_img.Magic, _img.Format, _img.BitDepth, loadContext.DialogManager);
                    Images = new List<IKanvasImage> { new KanvasImage(EncodingDefinition, loadedImg) };
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
                    _img.Save(fileStream, Images[0]);
                    break;
            }

            return Task.CompletedTask;
        }

        private bool IsContentChanged()
        {
            return Images.Any(x => x.ContentChanged);
        }
    }
}
