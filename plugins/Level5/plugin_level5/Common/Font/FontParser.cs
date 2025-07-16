using Konnect.Contract.Management.Dialog;
using Konnect.Contract.Management.Files;
using plugin_level5.Common.Archive;
using plugin_level5.Common.Archive.Models;
using plugin_level5.Common.Font.Models;
using plugin_level5.Common.Image;
using plugin_level5.Common.Image.Models;

namespace plugin_level5.Common.Font
{
    internal class FontParser
    {
        private readonly ArchiveTypeReader _archiveTypeReader = new();
        private readonly ArchiveReaderFactory _archiveReaderFactory = new();
        private readonly FontVersionReader _fontVersionReader = new();
        private readonly FontReaderFactory _fontReaderFactory = new();
        private readonly ImageParser _imageParser;

        public FontParser(IDialogManager dialogManager, IPluginFileManager fileManager)
        {
            _imageParser = new ImageParser(dialogManager, fileManager);
        }

        public async Task<FontImageData?> Parse(Stream input)
        {
            ArchiveData archiveData = ReadFiles(input);

            FontData? fontData = GetFontData(archiveData.Files);
            if (fontData == null)
                return null;

            ImageData[] glyphImages = await GetGlyphImages(archiveData.Files);

            return new FontImageData
            {
                Platform = fontData.Version.Platform,
                ArchiveType = archiveData.ArchiveType,
                Font = fontData,
                Images = glyphImages
            };
        }

        private ArchiveData ReadFiles(Stream input)
        {
            ArchiveType archiveType = _archiveTypeReader.Peek(input);
            IArchiveReader reader = _archiveReaderFactory.Create(archiveType);

            return reader.Read(input);
        }

        private FontData? GetFontData(IList<ArchiveNamedEntry> files)
        {
            ArchiveNamedEntry? fntFile = files.FirstOrDefault(f => f.Name == "FNT.bin");
            if (fntFile == null)
                return null;

            int fontVersion = _fontVersionReader.Peek(fntFile.Content);
            IFontReader fontReader = _fontReaderFactory.Create(fontVersion);

            return fontReader.Read(fntFile.Content);
        }

        private async Task<ImageData[]> GetGlyphImages(IList<ArchiveNamedEntry> files)
        {
            var result = new List<ImageData>();

            IEnumerable<ArchiveNamedEntry> imageFiles = files.Where(f => f.Name.EndsWith(".xi"));
            foreach (ArchiveNamedEntry imageFile in imageFiles)
            {
                ImageData imgData = await _imageParser.Parse(imageFile.Content);

                result.Add(imgData);
            }

            return result.ToArray();
        }
    }
}
