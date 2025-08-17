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
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Reflection;
using Kaligraphy.Contract.Layout;
using Kaligraphy.Contract.Rendering;
using Kaligraphy.DataClasses.Parsing;
using Kaligraphy.Enums.Layout;
using Serilog.Core;

namespace plugin_level5_preview.Preview
{
    class TimeTravelersState : IGamePluginState
    {
        private readonly IPluginFileManager _pluginManager;

        private IReadOnlyList<CharacterInfo>? _loadedFont;

        public ICharacterParser? Parser { get; }
        public ICharacterComposer? Composer { get; }
        public ICharacterSerializer? Serializer { get; }
        public ICharacterDeserializer? Deserializer { get; }

        public TimeTravelersState(IPluginFileManager pluginFileManager)
        {
            _pluginManager = pluginFileManager;
        }

        public async Task<IList<Image<Rgba32>>?> CreatePreviewPages(IList<IList<CharacterData>> characters)
        {
            IReadOnlyList<CharacterInfo>? font = await GetFont();
            if (font is null)
                return null;

            var glyphProvider = new FontPluginGlyphProvider(font);

            var isNarrator = IsNarrator(characters);
            var screen = GetScreen(isNarrator);

            var layouter = GetLayouter(isNarrator, glyphProvider);
            var renderer = GetRenderer(isNarrator, glyphProvider);

            var initPoint = isNarrator ? new Point(16, 1) : new Point(0, 15);
            foreach (IList<CharacterData> characterSet in characters)
            {
                var layout = layouter.Create(characterSet, initPoint, screen.Size);
                renderer.Render(screen, layout);

                initPoint = new Point(initPoint.X, initPoint.Y + layout.BoundingBox.Height);
            }

            return [screen];
        }

        private bool IsNarrator(IList<IList<CharacterData>> characters)
        {
            if (characters.Count <= 0 || characters[0].Count <= 0)
                return false;

            CharacterData character = characters[0][0];
            if (character is not FontCharacterData text)
                return false;

            return text.Character is '＊';
        }

        private Image<Rgba32> GetScreen(bool isNarrator)
        {
            if (isNarrator)
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
            else
            {
                var image = new Image<Rgba32>(400, 240);
                image.Mutate(x => x.Clear(Color.Wheat));

                return image;
            }
        }

        private ITextLayouter GetLayouter(bool isNarrator, IGlyphProvider glyphProvider)
        {
            if (isNarrator)
            {
                return new TextLayouter(new LayoutOptions
                {
                    HorizontalAlignment = HorizontalTextAlignment.Left,
                    VerticalAlignment = VerticalTextAlignment.Top,
                    LineHeight = 25,
                    LineWidth = 286
                }, glyphProvider);
            }

            return new TextLayouter(new LayoutOptions
            {
                HorizontalAlignment = HorizontalTextAlignment.Center,
                VerticalAlignment = VerticalTextAlignment.Bottom,
                LineHeight = 21,
                LineWidth = 286
            }, glyphProvider);
        }

        private ITextRenderer GetRenderer(bool isNarrator, IGlyphProvider glyphProvider)
        {
            if (isNarrator)
            {
                return new TextRenderer(new RenderOptions
                {
                    TextColor = Color.FromRgb(0xFD, 0xFD, 0xFD)
                }, glyphProvider);
            }

            return new TextRenderer(new RenderOptions
            {
                VisibleLines = 2,
                OutlineRadius = 3,
                TextColor = Color.FromRgb(0xCE, 0xCE, 0xCE),
                TextOutlineColor = Color.Black
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
