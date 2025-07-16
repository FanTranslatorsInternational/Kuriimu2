using Konnect.Contract.Management.Files;
using plugin_level5.Common.Archive;
using plugin_level5.Common.Archive.Models;
using plugin_level5.Common.Font.Models;
using plugin_level5.Common.Image;
using plugin_level5.Common.Image.Models;

namespace plugin_level5.Common.Font
{
    internal class FontComposer
    {
        private readonly IPluginFileManager _fileManager;
        private readonly FontWriterFactory _fontWriterFactory = new();
        private readonly ArchiveWriterFactory _archiveWriterFactory = new();

        public FontComposer(IPluginFileManager fileManager)
        {
            _fileManager = fileManager;
        }

        public async Task Compose(FontImageData data, Stream output)
        {
            var archiveData = new ArchiveData
            {
                ArchiveType = data.ArchiveType,
                ContentType = 1,
                Files = new List<ArchiveNamedEntry>(2)
            };

            await AddImages(archiveData.Files, data);
            AddFont(archiveData.Files, data);

            WriteFiles(archiveData, output);
        }

        private void WriteFiles(ArchiveData data, Stream output)
        {
            IArchiveWriter writer = _archiveWriterFactory.Create(data.ArchiveType);

            writer.Write(data, output);
        }

        private async Task AddImages(IList<ArchiveNamedEntry> files, FontImageData fontData)
        {
            var index = 0;
            foreach (ImageData image in fontData.Images)
            {
                var imageComposer = new ImageComposer(_fileManager);

                var imageStream = new MemoryStream();
                await imageComposer.Compose(image, imageStream);

                imageStream.Position = 0;
                files.Add(new ArchiveNamedEntry
                {
                    Name = $"{index++:000}.xi",
                    Content = imageStream
                });
            }
        }

        private void AddFont(IList<ArchiveNamedEntry> files, FontImageData data)
        {
            var fontStream = new MemoryStream();

            IFontWriter fontWriter = _fontWriterFactory.Create(data.Font.Version.Version);
            fontWriter.Write(data.Font, fontStream);

            fontStream.Position = 0;
            files.Add(new ArchiveNamedEntry
            {
                Name = "FNT.bin",
                Content = fontStream
            });
        }
    }
}
