using Kaligraphy.Contract.DataClasses.Layout;
using Kaligraphy.Contract.DataClasses.Parsing;
using Kaligraphy.Contract.Parsing;
using Kaligraphy.DataClasses.Layout;
using Kaligraphy.DataClasses.Rendering;
using Kaligraphy.Layout;
using Kaligraphy.Rendering;
using Konnect.Contract.DataClasses.Management.Files;
using Konnect.Contract.DataClasses.Plugin.File.Font;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File.Font;
using Konnect.Contract.Plugin.Game;
using Konnect.FileSystem;
using Konnect.Management.Streams;
using Konnect.Plugin.File.Font;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace plugin_mt_framework_preview.Texts
{
    class AceAttorney5State : IGamePluginState
    {
        private readonly IPluginFileManager _pluginManager;

        public ICharacterParser? Parser { get; } = new GmdCharacterParser();
        public ICharacterComposer? Composer { get; }
        public ICharacterSerializer? Serializer { get; } = new GmdCharacterSerializer();
        public ICharacterDeserializer? Deserializer { get; }

        public AceAttorney5State(IPluginFileManager pluginFileManager)
        {
            _pluginManager = pluginFileManager;
        }

        public async Task<Image<Rgba32>?> CreatePreview(IList<CharacterData> characters)
        {
            IReadOnlyList<CharacterInfo>? font = await GetFont();
            if (font is null)
                return null;

            var glyphProvider = new FontPluginGlyphProvider(font);
            var layouter = new TextLayouter(new LayoutOptions(), glyphProvider);
            var renderer = new TextRenderer(new RenderOptions(), glyphProvider);

            IList<TextLayoutLineData> layoutLines = layouter.Create(characters);

            int imageWidth = layoutLines.Count <= 0 ? 0 : layoutLines.Max(l => l.BoundingBox.Width);
            int imageHeight = layoutLines.Count <= 0 ? 0 : layoutLines.Sum(l => l.BoundingBox.Height);
            if (imageWidth <= 0 || imageHeight <= 0)
                return null;

            var image = new Image<Rgba32>(imageWidth + 1, imageHeight + 1);
            TextLayoutData layout = layouter.Create(layoutLines, image.Size);

            renderer.Render(image, layout);

            return image;
        }

        private async Task<IReadOnlyList<CharacterInfo>?> GetFont()
        {
            string resourcePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", "ace_attorney_5");
            if (!Directory.Exists(resourcePath))
                return null;

            IFileSystem fileSystem = FileSystemFactory.CreateSubFileSystem(resourcePath, new StreamManager());
            LoadResult loadResult = await _pluginManager.LoadFile(fileSystem, "font00_eng.gfd", Guid.Parse("e95928dd-31b9-445c-afbd-d692c694abae"));

            var fontState = loadResult.LoadedFileState?.PluginState as IFontFilePluginState;
            IReadOnlyList<CharacterInfo>? characters = fontState?.Characters;
            if (characters is null)
                return null;

            _pluginManager.Close(loadResult.LoadedFileState!);

            return characters;
        }
    }
}
