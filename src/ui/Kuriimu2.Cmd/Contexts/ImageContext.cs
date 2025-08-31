using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Kanvas;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File.Image;
using Konnect.Contract.Progress;
using Konnect.Extensions;
using Konnect.FileSystem;
using Konnect.Management.Streams;
using Kuriimu2.Cmd.Models.Contexts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Kuriimu2.Cmd.Contexts
{
    class ImageContext : BaseContext
    {
        private readonly IFileState _stateInfo;
        private readonly IImageFilePluginState _imageState;
        private readonly IContext _parentContext;

        public ImageContext(IFileState stateInfo, IContext parentContext, IProgressContext progressContext) :
            base(progressContext)
        {
            _stateInfo = stateInfo;
            _imageState = _stateInfo.PluginState.Image!;
            _parentContext = parentContext;
        }

        protected override Command[] GetCommandsInternal()
        {
            return
            [
                new Command("list"),
                new Command("print", "image-index"),
                new Command("extract", "image-index", "file-path"),
                new Command("extract-all", "directory-path"),
                new Command("inject", "image-index", "file-path"),
                new Command("inject-all", "directory-path"),
                new Command("back")
            ];
        }

        protected override Task<IContext?> ExecuteNextInternal(Command command, IList<string> arguments)
        {
            switch (command.Name)
            {
                case "list":
                    ListImages();
                    return Task.FromResult<IContext?>(this);

                case "print":
                    PrintImage(arguments[0]);
                    return Task.FromResult<IContext?>(this);

                case "extract":
                    ExtractImage(arguments[0], arguments[1]);
                    return Task.FromResult<IContext?>(this);

                case "extract-all":
                    ExtractAllImage(arguments[0]);
                    return Task.FromResult<IContext?>(this);

                case "inject":
                    InjectImage(arguments[0], arguments[1]);
                    return Task.FromResult<IContext?>(this);

                case "inject-all":
                    InjectAllImage(arguments[0]);
                    return Task.FromResult<IContext?>(this);

                case "back":
                    return Task.FromResult<IContext?>(_parentContext);
            }

            return Task.FromResult<IContext?>(null);
        }

        private void ExtractImage(string imageIndexArgument, UPath filePath)
        {
            if (!int.TryParse(imageIndexArgument, out int imageIndex))
            {
                Console.WriteLine($"'{imageIndexArgument}' is not a valid number.");
                return;
            }

            if (imageIndex < 0 || imageIndex >= _imageState.Images.Count)
            {
                Console.WriteLine($"Index '{imageIndex}' was out of bounds.");
                return;
            }

            IFileSystem destinationFileSystem = FileSystemFactory.CreateSubFileSystem(filePath.GetDirectory().FullName, new StreamManager());
            ExtractImageInternal(_imageState.Images[imageIndex], destinationFileSystem, filePath.GetName());
        }

        private void ExtractAllImage(UPath directoryPath)
        {
            IFileSystem destinationFileSystem = FileSystemFactory.CreateSubFileSystem(directoryPath.FullName, new StreamManager());

            for (var i = 0; i < _imageState.Images.Count; i++)
            {
                string? imageFileName = _imageState.Images[i].ImageInfo.Name;
                if (string.IsNullOrEmpty(imageFileName))
                    imageFileName = _stateInfo.FilePath.GetNameWithoutExtension() + $".{i:00}";

                ExtractImageInternal(_imageState.Images[i], destinationFileSystem, imageFileName + ".png");
            }
        }

        private void ExtractImageInternal(IImageFile image, IFileSystem destinationFileSystem, string fileName)
        {
            using Stream newFileStream = destinationFileSystem.OpenFile(fileName, FileMode.Create, FileAccess.Write);

            image.GetImage(Progress).SaveAsPng(newFileStream);
        }

        private void InjectImage(string imageIndexArgument, UPath injectPath)
        {
            if (!int.TryParse(imageIndexArgument, out int imageIndex))
            {
                Console.WriteLine($"'{imageIndexArgument}' is not a valid number.");
                return;
            }

            if (imageIndex < 0 || imageIndex >= _imageState.Images.Count)
            {
                Console.WriteLine($"Index '{imageIndex}' was out of bounds.");
                return;
            }

            IImageFile imageFile = _imageState.Images[imageIndex];
            imageFile.SetImage(Image.Load<Rgba32>(injectPath.FullName), Progress);
        }

        private void InjectAllImage(UPath directoryPath)
        {
            IFileSystem destinationFileSystem = FileSystemFactory.CreateSubFileSystem(directoryPath.FullName, new StreamManager());

            for (var i = 0; i < _imageState.Images.Count; i++)
            {
                string? imageFileName = _imageState.Images[i].ImageInfo.Name;
                if (string.IsNullOrEmpty(imageFileName))
                    imageFileName = _stateInfo.FilePath.GetNameWithoutExtension() + $".{i:00}";

                imageFileName += ".png";

                if (!destinationFileSystem.FileExists(imageFileName))
                    continue;

                Stream input = destinationFileSystem.OpenFile(imageFileName);
                Image<Rgba32> newImage = Image.Load<Rgba32>(input);

                _imageState.Images[i].SetImage(newImage, Progress);
            }
        }

        private void ListImages()
        {
            for (var i = 0; i < _imageState.Images.Count; i++)
            {
                string? imageFileName = _imageState.Images[i].ImageInfo.Name;
                if (string.IsNullOrEmpty(imageFileName))
                    imageFileName = $"{i:00}";

                string saveIndicator = _imageState.Images[i].ImageInfo.ContentChanged ? "* " : string.Empty;
                Console.WriteLine($"[{i}] " + saveIndicator + imageFileName);

                int mipMapCount = _imageState.Images[i].ImageInfo.MipMapData?.Count ?? 0;
                if (mipMapCount > 0)
                    Console.WriteLine($"  Image contains {mipMapCount} mip maps.");
            }
        }

        private void PrintImage(string imageIndexArgument)
        {
            if (!int.TryParse(imageIndexArgument, out int imageIndex))
            {
                Console.WriteLine($"'{imageIndexArgument}' is not a valid number.");
                return;
            }

            if (imageIndex < 0 || imageIndex >= _imageState.Images.Count)
            {
                Console.WriteLine($"Index '{imageIndex}' was out of bounds.");
                return;
            }

            IImageFile image = _imageState.Images[imageIndex];

            var newSize = new Size(Console.WindowWidth, Console.WindowHeight);
            Image<Rgba32> decodedImage = image.GetImage(Progress);
            Console.WriteLine();

            Image<Rgba32> resizedImage = decodedImage.Clone(x => x.Resize(newSize));

            string asciiImage = ConvertAscii(resizedImage);
            Console.WriteLine(asciiImage);
        }


        // https://www.c-sharpcorner.com/article/generating-ascii-art-from-an-image-using-C-Sharp/
        private static string ConvertAscii(Image<Rgba32> image)
        {
            var asciiChars = new[] { '#', '#', '@', '%', '=', '+', '*', ':', '-', '.', ' ' };
            var sb = new StringBuilder(image.Width * image.Height);

            foreach (Rgba32 color in image.ToColors())
            {
                int luminance = (color.R + color.G + color.B) / 3;
                int asciiIndex = luminance * 10 / 255;

                sb.Append(asciiChars[asciiIndex]);
            }

            return sb.ToString();
        }
    }
}
