using System.Reflection;
using Kaligraphy.Contract.DataClasses.Layout;
using Kaligraphy.Contract.DataClasses.Parsing;
using Kaligraphy.DataClasses.Layout;
using Kaligraphy.DataClasses.Parsing;
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
using plugin_mt_framework_preview.Characters;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace plugin_mt_framework_preview.Previews
{
    class AceAttorney5State : GenericGmdState, ITextPreviewState
    {
        private readonly IPluginFileManager _pluginManager;

        public AceAttorney5State(IPluginFileManager pluginFileManager)
        {
            _pluginManager = pluginFileManager;
        }

        public async Task<IList<Image<Rgba32>>?> RenderPreviews(IList<IList<CharacterData>> characters)
        {
            IReadOnlyList<CharacterInfo>? font = await GetFont();
            if (font is null)
                return null;

            Image<Rgba32>? dialogueBox = GetDialogueBox();
            if (dialogueBox is null)
                return null;

            var glyphProvider = new FontPluginGlyphProvider(font);
            var layouter = new TextLayouter(new LayoutOptions { LineHeight = 24 }, glyphProvider);
            var renderer = new TextRenderer(new RenderOptions(), glyphProvider);

            var result = new List<Image<Rgba32>>();

            foreach (IList<CharacterData> characterSet in characters)
            {
                List<IList<CharacterData>> pages = GetPageCharacters(characterSet);

                var initPoint = new Point(16, 29);
                foreach (IList<CharacterData> page in pages)
                {
                    Image<Rgba32> image = dialogueBox.Clone();
                    TextLayoutData layout = layouter.Create(page, initPoint, dialogueBox.Size);

                    renderer.Render(image, layout);

                    result.Add(image);
                }
            }

            return result;
        }

        private static List<IList<CharacterData>> GetPageCharacters(IList<CharacterData> characters)
        {
            var result = new List<IList<CharacterData>>();
            result.Add([]);

            var hasText = false;
            foreach (CharacterData character in characters)
            {
                if (character is GmdControlCodeCharacterData { Code: "PAGE" })
                {
                    result.Add([]);
                    hasText = false;

                    continue;
                }

                if (character is LineBreakCharacterData && !hasText)
                    continue;

                if (character is FontCharacterData)
                    hasText = true;

                result[^1].Add(character);
            }

            return result;
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

        private Image<Rgba32>? GetDialogueBox()
        {
            Stream? boxStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("aa5_box.png");
            if (boxStream is null)
                return null;

            return Image.Load<Rgba32>(boxStream);
        }
    }
}
