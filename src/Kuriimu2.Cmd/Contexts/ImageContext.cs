using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Kanvas;
using Kontract;
using Kontract.Extensions;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models.IO;
using Kore.Factories;
using Kore.Managers;

namespace Kuriimu2.Cmd.Contexts
{
    class ImageContext : BaseContext
    {
        private readonly IStateInfo _stateInfo;
        private readonly IImageState _imageState;
        private readonly IContext _parentContext;

        protected override IList<Command> Commands { get; } = new List<Command>
        {
            new Command("print","image-index"),
            new Command("extract", "image-index", "file-path"),
            new Command("extract-all", "directory-path"),
            new Command("inject", "image-index", "file-path"),
            new Command("list"),
            new Command("back")
        };

        public ImageContext(IStateInfo stateInfo, IContext parentContext)
        {
            ContractAssertions.IsNotNull(stateInfo, nameof(stateInfo));
            ContractAssertions.IsNotNull(parentContext, nameof(parentContext));

            _stateInfo = stateInfo;
            _imageState = _stateInfo.PluginState as IImageState;
            _parentContext = parentContext;

        }

        protected override Task<IContext> ExecuteNextInternal(Command command, IList<string> arguments)
        {
            switch (command.Name)
            {
                case "print":
                    PrintImage(arguments[0]);
                    return Task.FromResult((IContext)this);

                case "extract":
                    ExtractImage(arguments[0], arguments[1]);
                    return Task.FromResult((IContext)this);

                case "extract-all":
                    ExtractAllImage(arguments[0]);
                    return Task.FromResult((IContext)this);

                case "inject":
                    InjectImage(arguments[0], arguments[1]);
                    return Task.FromResult((IContext)this);

                case "list":
                    ListImages();
                    return Task.FromResult((IContext)this);

                case "back":
                    return Task.FromResult(_parentContext);
            }

            return null;
        }

        private void ExtractImage(string imageIndexArgument, UPath filePath)
        {
            if (!int.TryParse(imageIndexArgument, out var imageIndex))
            {
                Console.WriteLine($"'{imageIndexArgument}' is not a valid number.");
                return;
            }

            if (imageIndex >= _imageState.Images.Count)
            {
                Console.WriteLine($"Index '{imageIndex}' was out of bounds.");
                return;
            }

            var destinationFileSystem = FileSystemFactory.CreatePhysicalFileSystem(filePath.GetDirectory(), new StreamManager());
            ExtractImageInternal(new KanvasImage(_imageState, _imageState.Images[imageIndex]), destinationFileSystem, filePath.GetName());
        }

        private void ExtractAllImage(UPath directoryPath)
        {
            var destinationFileSystem = FileSystemFactory.CreatePhysicalFileSystem(directoryPath, new StreamManager());

            for (var i = 0; i < _imageState.Images.Count; i++)
            {
                var imageFileName = _imageState.Images[i].Name;
                if (string.IsNullOrEmpty(imageFileName))
                    imageFileName = _stateInfo.FilePath.GetNameWithoutExtension() + $".{i:00}";

                ExtractImageInternal(new KanvasImage(_imageState, _imageState.Images[i]), destinationFileSystem, imageFileName + ".png");
            }
        }

        private void ExtractImageInternal(KanvasImage image, IFileSystem destinationFileSystem, string fileName)
        {
            var newFileStream = destinationFileSystem.OpenFile(fileName, FileMode.Create, FileAccess.Write);

            // TODO: Allow progress?
            var imageStream = new MemoryStream();
            image.GetImage().Save(imageStream, ImageFormat.Png);

            imageStream.Position = 0;
            imageStream.CopyTo(newFileStream);

            imageStream.Close();
            newFileStream.Close();
        }

        private void InjectImage(string imageIndexArgument, UPath injectPath)
        {
            if (!int.TryParse(imageIndexArgument, out var imageIndex))
            {
                Console.WriteLine($"'{imageIndexArgument}' is not a valid number.");
                return;
            }

            if (imageIndex >= _imageState.Images.Count)
            {
                Console.WriteLine($"Index '{imageIndex}' was out of bounds.");
                return;
            }

            var kanvasImage = new KanvasImage(_imageState, _imageState.Images[imageIndex]);
            kanvasImage.SetImage((Bitmap)Image.FromFile(injectPath.FullName));
        }

        private void ListImages()
        {
            for (var i = 0; i < _imageState.Images.Count; i++)
            {
                var imageFileName = _imageState.Images[i].Name;
                if (string.IsNullOrEmpty(imageFileName))
                    imageFileName = $"{i:00}";

                var saveIndicator = _imageState.Images[i].ContentChanged ? "* " : string.Empty;
                Console.WriteLine($"[{i}] " + saveIndicator + imageFileName);

                if (_imageState.Images[i].MipMapCount > 0)
                    Console.WriteLine($"  Image contains {_imageState.Images[i].MipMapCount} mip maps.");
            }
        }

        private void PrintImage(string imageIndexArgument)
        {
            if (!int.TryParse(imageIndexArgument, out var imageIndex))
            {
                Console.WriteLine($"'{imageIndexArgument}' is not a valid number.");
                return;
            }

            if (imageIndex >= _imageState.Images.Count)
            {
                Console.WriteLine($"Index '{imageIndex}' was out of bounds.");
                return;
            }

            var image = new KanvasImage(_imageState, _imageState.Images[imageIndex]);

            var newSize = new Size(Console.WindowWidth, Console.WindowHeight);
            var resizedImage = ResizeImage(image.GetImage(), newSize);

            var asciiImage = ConvertAscii(resizedImage);
            Console.WriteLine(asciiImage);
        }

        private Bitmap ResizeImage(Image img, Size newSize)
        {
            var newImg = new Bitmap(newSize.Width, newSize.Height);
            using var g = Graphics.FromImage(newImg);

            g.CompositingMode = CompositingMode.SourceCopy;
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            g.DrawImage(img, new Rectangle(Point.Empty, newSize), new Rectangle(Point.Empty, img.Size), GraphicsUnit.Pixel);

            return newImg;
        }

        // https://www.c-sharpcorner.com/article/generating-ascii-art-from-an-image-using-C-Sharp/
        private string ConvertAscii(Bitmap image)
        {
            var asciiChars = new[] { '#', '#', '@', '%', '=', '+', '*', ':', '-', '.', ' ' };
            var sb = new StringBuilder(image.Width * image.Height);

            foreach (var color in image.ToColors())
            {
                var grayValue = (color.R + color.G + color.B) / 3;
                var asciiIndex = grayValue * 10 / 255;
                sb.Append(asciiChars[asciiIndex]);
            }

            return sb.ToString();
        }
    }
}
