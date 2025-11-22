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
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Reflection;
using Kaligraphy.Contract.Layout;
using Kaligraphy.Contract.Rendering;
using Kaligraphy.Enums.Layout;
using Serilog.Core;

namespace plugin_level5_preview.Preview.Narration
{
    class TimeTravelersNarrationState : ITextPreviewState
    {
        private readonly IPluginFileManager _pluginManager;

        private IReadOnlyList<CharacterInfo>? _loadedFont;

        public ICharacterParser? Parser { get; } = new NarrationCharacterParser();
        public ICharacterComposer? Composer { get; } = new TimeTravelersCharacterComposer();
        public ICharacterSerializer? Serializer { get; } = new TimeTravelersCharacterSerializer();
        public ICharacterDeserializer? Deserializer { get; } = new NarrationCharacterDeserializer();

        public TimeTravelersNarrationState(IPluginFileManager pluginFileManager)
        {
            _pluginManager = pluginFileManager;
        }

        public async Task<IList<Image<Rgba32>>?> RenderPreviews(IList<IList<Kaligraphy.Contract.DataClasses.Parsing.CharacterData>> characters)
        {
            IReadOnlyList<CharacterInfo>? font = await GetFont();
            if (font is null)
                return null;

            var glyphProvider = new FontPluginGlyphProvider(font);

            var screen = GetScreen();

            var layouter = GetLayouter(glyphProvider);
            var renderer = GetRenderer(glyphProvider);

            var initPoint = new Point(16, 1);
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
            var image = new Image<Rgba32>(320, 240);
            image.Mutate(x => x.Clear(Color.Black));

            Image<Rgba32>? narrationImage = GetNarrationResource();
            if (narrationImage == null)
                return image;

            var narrationPoint = new Point(0, image.Height - narrationImage.Height);
            image.Mutate(x => x.DrawImage(narrationImage, narrationPoint, 1f));

            return image;
        }

        private ITextLayouter GetLayouter(IGlyphProvider glyphProvider)
        {
            return new TextLayouter(new LayoutOptions
            {
                HorizontalAlignment = HorizontalTextAlignment.Left,
                VerticalAlignment = VerticalTextAlignment.Top,
                LineHeight = 25,
                LineWidth = 286
            }, glyphProvider);
        }

        private ITextRenderer GetRenderer(IGlyphProvider glyphProvider)
        {
            return new TextRenderer(new RenderOptions
            {
                TextColor = Color.FromRgb(0xFD, 0xFD, 0xFD)
            }, glyphProvider);
        }

        private async Task<IReadOnlyList<CharacterInfo>?> GetFont()
        {
            if (_loadedFont is not null)
                return _loadedFont;

            string resourcePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", "time_travelers");
            if (!Directory.Exists(resourcePath))
                return null;

            IFileSystem fileSystem = FileSystemFactory.CreateSubFileSystem(resourcePath, new StreamManager());
            LoadResult loadResult = await _pluginManager.LoadFile(fileSystem, "nrm_main.xf", new LoadFileContext
            {
                Logger = Logger.None,
                Options = { "Time Travelers" },
                PluginId = Guid.Parse("b1b397c4-9a02-4828-b568-39cad733fa3a")
            });

            var fontState = loadResult.LoadedFileState?.PluginState as IFontFilePluginState;
            IReadOnlyList<CharacterInfo>? characters = fontState?.Characters;
            if (characters is null)
                return null;

            _pluginManager.Close(loadResult.LoadedFileState!);

            return _loadedFont = characters;
        }

        private Image<Rgba32>? GetNarrationResource()
        {
            Stream? boxStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("narration.png");
            if (boxStream is null)
                return null;

            return Image.Load<Rgba32>(boxStream);
        }
    }
}
