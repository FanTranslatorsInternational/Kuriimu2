using Kaligraphy.Contract.Parsing;
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
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Kaligraphy.Contract.Layout;
using Kaligraphy.Contract.Rendering;
using Kaligraphy.Enums.Layout;
using Serilog.Core;

namespace plugin_level5_preview.Preview.Subtitle
{
    class TimeTravelersSubtitleState : ITextPreviewState
    {
        private readonly IPluginFileManager _pluginManager;

        private IReadOnlyList<FontSet>? _loadedFont;

        public ICharacterParser? Parser { get; } = new SubtitleCharacterParser();
        public ICharacterComposer? Composer { get; } = new TimeTravelersCharacterComposer();
        public ICharacterSerializer? Serializer { get; } = new TimeTravelersCharacterSerializer();
        public ICharacterDeserializer? Deserializer { get; } = new SubtitleCharacterDeserializer();

        public TimeTravelersSubtitleState(IPluginFileManager pluginFileManager)
        {
            _pluginManager = pluginFileManager;
        }

        public async Task<IList<Image<Rgba32>>?> RenderPreviews(IList<IList<Kaligraphy.Contract.DataClasses.Parsing.CharacterData>> characters)
        {
            IReadOnlyList<FontSet>? sets = await GetFontSets();
            if (sets is null)
                return null;

            var glyphProvider = new FontPluginGlyphProvider(sets[0].Characters);
            var furiganaGlyphProvider = new FontPluginGlyphProvider(sets[1].Characters);

            var screen = GetScreen();

            var layouter = GetLayouter(glyphProvider, furiganaGlyphProvider);
            var renderer = GetRenderer(glyphProvider, furiganaGlyphProvider);

            var initPoint = new Point(0, 15);
            foreach (IList<Kaligraphy.Contract.DataClasses.Parsing.CharacterData> characterSet in characters)
            {
                var layout = layouter.Create(characterSet, initPoint, screen.Size);
                renderer.Render(screen, layout);

                initPoint = new Point(initPoint.X, initPoint.Y + (int)layout.BoundingBox.Height);
            }

            return [screen];
        }

        private Image<Rgba32> GetScreen()
        {
            var image = new Image<Rgba32>(400, 240);
            image.Mutate(x => x.Clear(Color.Wheat));

            return image;
        }

        private ITextLayouter GetLayouter(IGlyphProvider glyphProvider, IGlyphProvider furiganaProvider)
        {
            return new FuriganaTextLayouter(new FuriganaLayoutOptions
            {
                HorizontalAlignment = HorizontalTextAlignment.Center,
                VerticalAlignment = VerticalTextAlignment.Bottom,
                LineHeight = 21,
                LineWidth = 290,
                FuriganaLineSpacing = -2
            }, glyphProvider, furiganaProvider);
        }

        private ITextRenderer GetRenderer(IGlyphProvider glyphProvider, IGlyphProvider furiganaProvider)
        {
            return new FuriganaTextRenderer(new FuriganaRenderOptions
            {
                VisibleLines = 2,
                OutlineRadius = 3,
                TextColor = Color.FromRgb(0xCE, 0xCE, 0xCE),
                TextOutlineColor = Color.Black,
                FuriganaTextColor = Color.FromRgb(0xCE, 0xCE, 0xCE)
            }, glyphProvider, furiganaProvider);
        }

        private async Task<IReadOnlyList<FontSet>?> GetFontSets()
        {
            if (_loadedFont is not null)
                return _loadedFont;

            string resourcePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", "time_travelers");
            if (!Directory.Exists(resourcePath))
                return null;

            IFileSystem fileSystem = FileSystemFactory.CreatePhysicalSubFileSystem(resourcePath, new StreamManager());
            LoadResult loadResult = await _pluginManager.LoadFile(fileSystem, "nrm_main.xf", new LoadFileContext
            {
                Logger = Logger.None,
                Options = { "Time Travelers" },
                PluginId = Guid.Parse("b1b397c4-9a02-4828-b568-39cad733fa3a")
            });

            var fontState = loadResult.LoadedFileState?.PluginState as IFontFilePluginState;
            IReadOnlyList<FontSet>? sets = fontState?.Sets;
            if (sets is null)
                return null;

            _pluginManager.Close(loadResult.LoadedFileState!);

            return _loadedFont = sets;
        }
    }
}
