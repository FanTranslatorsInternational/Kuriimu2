using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Kaligraphy.Contract.DataClasses.Parsing;
using Kaligraphy.Contract.Parsing;
using Kaligraphy.Parsing;
using Konnect.Contract.DataClasses.Plugin.File.Text;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Management.Plugin;
using Konnect.Contract.Plugin.File.Text;
using Konnect.Contract.Plugin.Game;
using Konnect.Contract.Progress;
using Konnect.DataClasses.Management.Text;
using Konnect.Management.Text;
using Kuriimu2.Cmd.Models.Contexts;

namespace Kuriimu2.Cmd.Contexts
{
    class TextContext : BaseContext
    {
        private readonly IPluginManager _pluginManager;
        private readonly IFileState _stateInfo;
        private readonly ITextFilePluginState _textState;
        private readonly IContext _parentContext;

        public TextContext(IFileState stateInfo, IContext parentContext, IPluginManager pluginManager, IProgressContext progressContext) :
            base(progressContext)
        {
            _pluginManager = pluginManager;
            _stateInfo = stateInfo;
            _textState = _stateInfo.PluginState.Text!;
            _parentContext = parentContext;
        }

        protected override Command[] GetCommandsInternal()
        {
            return
            [
                new Command("list"),
                new Command("list-games"),
                new Command("print", "text-index"),
                new Command("print-with", "text-index", "game-plugin-id"),
                new Command("extract-all", "format", "file-path"),
                new Command("extract-all-with", "format", "file-path", "game-plugin-id"),
                new Command("inject-all", "file-path"),
                new Command("inject-all-with", "file-path", "game-plugin-id"),
                new Command("back")
            ];
        }

        protected override Task<IContext?> ExecuteNextInternal(Command command, IList<string> arguments)
        {
            switch (command.Name)
            {
                case "list":
                    ListTexts();
                    return Task.FromResult<IContext?>(this);

                case "list-games":
                    ListGames();
                    return Task.FromResult<IContext?>(this);

                case "print":
                    PrintText(arguments[0], null);
                    return Task.FromResult<IContext?>(this);

                case "print-with":
                    PrintText(arguments[0], arguments[1]);
                    return Task.FromResult<IContext?>(this);

                case "extract-all":
                    ExtractAllText(arguments[0], arguments[1], null);
                    return Task.FromResult<IContext?>(this);

                case "extract-all-with":
                    ExtractAllText(arguments[0], arguments[1], arguments[2]);
                    return Task.FromResult<IContext?>(this);

                case "inject-all":
                    InjectAllText(arguments[0], null);
                    return Task.FromResult<IContext?>(this);

                case "inject-all-with":
                    InjectAllText(arguments[0], arguments[1]);
                    return Task.FromResult<IContext?>(this);

                case "back":
                    return Task.FromResult<IContext?>(_parentContext);
            }

            return Task.FromResult<IContext?>(null);
        }

        private void ListTexts()
        {
            var pageLookup = new Dictionary<string, int>();
            var entryLookup = new Dictionary<string, int>();

            ITextEntryPager? pager = _textState.Pager;
            if (pager is not null)
            {
                var index = 0;

                TextEntryPage[] pages = pager.Page(_textState.Texts);
                for (var i = 0; i < pages.Length; i++)
                {
                    string pageName = CreatePageName(pages[i], i, pageLookup);
                    Console.WriteLine("    " + pageName);

                    for (var j = 0; j < pages[i].Entries.Count; j++)
                    {
                        string entryName = CreateEntryName(pages[i].Entries[j], j, entryLookup);

                        string saveIndicator = pages[i].Entries[j].ContentChanged ? "* " : string.Empty;
                        Console.WriteLine($"[{index++}] " + saveIndicator + entryName);
                    }
                }
            }
            else
            {
                for (var i = 0; i < _textState.Texts.Count; i++)
                {
                    string entryName = CreateEntryName(_textState.Texts[i], i, entryLookup);

                    string saveIndicator = _textState.Texts[i].ContentChanged ? "* " : string.Empty;
                    Console.WriteLine($"[{i}] " + saveIndicator + entryName);
                }
            }
        }

        private void ListGames()
        {
            IGamePlugin[] gamePlugins = _pluginManager.GetPlugins<IGamePlugin>().ToArray();

            var preferredPlugins = new List<IGamePlugin>();
            foreach (Guid preferredPluginId in _textState.PreviewGuids ?? [])
            {
                IGamePlugin? gamePlugin = gamePlugins.FirstOrDefault(x => x.PluginId == preferredPluginId);
                if (gamePlugin is null)
                    continue;

                Console.WriteLine($"[{preferredPluginId}] - * {gamePlugin.Metadata.Name}");

                preferredPlugins.Add(gamePlugin);
            }

            foreach (IGamePlugin gamePlugin in gamePlugins.Except(preferredPlugins))
            {
                Console.WriteLine($"[{gamePlugin.PluginId}] -   {gamePlugin.Metadata.Name}");
            }
        }

        private void PrintText(string textIndexArgument, string? gamePluginIdArgument)
        {
            if (!int.TryParse(textIndexArgument, out int textIndex))
            {
                Console.WriteLine($"'{textIndexArgument}' is not a valid number.");
                return;
            }

            if (textIndex < 0 || textIndex >= _textState.Texts.Count)
            {
                Console.WriteLine($"Index '{textIndex}' was out of bounds.");
                return;
            }

            if (!TryParseGuidArgument(gamePluginIdArgument, out Guid pluginId))
            {
                Console.WriteLine($"'{gamePluginIdArgument}' is not a valid plugin ID.");
                return;
            }

            var gamePlugin = _pluginManager.GetPlugin<IGamePlugin>(pluginId);

            ITextEntryPager? pager = _textState.Pager;
            if (pager is not null)
            {
                var index = 0;

                TextEntryPage[] pages = pager.Page(_textState.Texts);
                foreach (TextEntryPage page in pages)
                {
                    if (textIndex >= index + page.Entries.Count)
                    {
                        index += page.Entries.Count;
                        continue;
                    }

                    TextEntry entry = page.Entries[textIndex - index];

                    IGamePluginState? gameState = gamePlugin?.CreatePluginState(_stateInfo.FilePath, page.Entries.AsReadOnly(), _stateInfo.FileManager);

                    ICharacterParser parser = gameState?.TextProcessing?.Parser ?? new CharacterParser();
                    ICharacterSerializer serializer = gameState?.TextProcessing?.Serializer ?? new CharacterSerializer();

                    IList<CharacterData> parsedCharacters = parser.Parse(entry.TextData, entry.Encoding);
                    string text = serializer.Serialize(parsedCharacters, true);

                    Console.WriteLine(text);
                }
            }
            else
            {
                TextEntry entry = _textState.Texts[textIndex];

                IGamePluginState? gameState = gamePlugin?.CreatePluginState(_stateInfo.FilePath, _textState.Texts, _stateInfo.FileManager);

                ICharacterParser parser = gameState?.TextProcessing?.Parser ?? new CharacterParser();
                ICharacterSerializer serializer = gameState?.TextProcessing?.Serializer ?? new CharacterSerializer();

                IList<CharacterData> parsedCharacters = parser.Parse(entry.TextData, entry.Encoding);
                string text = serializer.Serialize(parsedCharacters, true);

                Console.WriteLine(text);
            }
        }

        private void ExtractAllText(string formatArgument, string filePathArgument, string? gamePluginIdArgument)
        {
            if (formatArgument is not "po" and not "kup")
            {
                Console.WriteLine($"'{formatArgument}' is not a valid output format. Use 'po' or 'kup' instead.");
                return;
            }

            if (Directory.Exists(filePathArgument))
            {
                Console.WriteLine($"'{filePathArgument}' is a directory.");
                return;
            }

            if (!TryParseGuidArgument(gamePluginIdArgument, out Guid pluginId))
            {
                Console.WriteLine($"'{gamePluginIdArgument}' is not a valid plugin ID.");
                return;
            }

            var gamePlugin = _pluginManager.GetPlugin<IGamePlugin>(pluginId);

            List<TranslationFileEntry> entries = [];

            var pageLookup = new Dictionary<string, int>();
            var entryLookup = new Dictionary<string, int>();

            ITextEntryPager? pager = _textState.Pager;
            if (pager is not null)
            {
                TextEntryPage[] pages = pager.Page(_textState.Texts);
                for (var i = 0; i < pages.Length; i++)
                {
                    string pageName = CreatePageName(pages[i], i, pageLookup);

                    for (var j = 0; j < pages[i].Entries.Count; j++)
                    {
                        string entryName = CreateEntryName(pages[i].Entries[j], j, entryLookup);

                        IGamePluginState? gameState = gamePlugin?.CreatePluginState(_stateInfo.FilePath, _textState.Texts, _stateInfo.FileManager);

                        ICharacterParser parser = gameState?.TextProcessing?.Parser ?? new CharacterParser();
                        ICharacterSerializer serializer = gameState?.TextProcessing?.Serializer ?? new CharacterSerializer();

                        IList<CharacterData> parsedCharacters = parser.Parse(pages[i].Entries[j].TextData, pages[i].Entries[j].Encoding);
                        string text = serializer.Serialize(parsedCharacters, true);

                        entries.Add(new TranslationFileEntry
                        {
                            Name = entryName,
                            PageName = pageName,
                            OriginalText = text,
                            TranslatedText = text
                        });
                    }
                }
            }
            else
            {
                for (var i = 0; i < _textState.Texts.Count; i++)
                {
                    string entryName = CreateEntryName(_textState.Texts[i], i, entryLookup);

                    IGamePluginState? gameState = gamePlugin?.CreatePluginState(_stateInfo.FilePath, _textState.Texts, _stateInfo.FileManager);

                    ICharacterParser parser = gameState?.TextProcessing?.Parser ?? new CharacterParser();
                    ICharacterSerializer serializer = gameState?.TextProcessing?.Serializer ?? new CharacterSerializer();

                    IList<CharacterData> parsedCharacters = parser.Parse(_textState.Texts[i].TextData, _textState.Texts[i].Encoding);
                    string text = serializer.Serialize(parsedCharacters, true);

                    entries.Add(new TranslationFileEntry
                    {
                        Name = entryName,
                        PageName = null,
                        OriginalText = text,
                        TranslatedText = text
                    });
                }
            }

            if (entries.Count <= 0)
                return;

            Stream output;
            switch (formatArgument)
            {
                case "po":
                    output = File.Create(filePathArgument);
                    PoManager.Save(output, [.. entries]);

                    output.Close();
                    break;

                case "kup":
                    output = File.Create(filePathArgument);
                    KupManager.Save(output, [.. entries]);

                    output.Close();
                    break;
            }

            Console.WriteLine("Extracted successfully.");
        }

        private void InjectAllText(string filePathArgument, string? gamePluginIdArgument)
        {
            if (!File.Exists(filePathArgument))
            {
                Console.WriteLine($"'{filePathArgument}' does not exist.");
                return;
            }

            if (!TryParseGuidArgument(gamePluginIdArgument, out Guid pluginId))
            {
                Console.WriteLine($"'{gamePluginIdArgument}' is not a valid plugin ID.");
                return;
            }

            using Stream inputStream = File.OpenRead(filePathArgument);

            TranslationFileEntry[] entries = KupManager.Load(inputStream);
            if (entries.Length <= 0)
            {
                inputStream.Position = 0;

                entries = PoManager.Load(inputStream);
                if (entries.Length <= 0)
                {
                    Console.WriteLine($"No text to import from '{filePathArgument}'.");
                    return;
                }
            }

            var gamePlugin = _pluginManager.GetPlugin<IGamePlugin>(pluginId);

            var pageLookup = new Dictionary<string, int>();
            var entryLookup = new Dictionary<string, int>();

            ITextEntryPager? pager = _textState.Pager;
            if (pager is not null)
            {
                TextEntryPage[] pages = pager.Page(_textState.Texts);
                for (var i = 0; i < pages.Length; i++)
                {
                    string pageName = CreatePageName(pages[i], i, pageLookup);

                    for (var j = 0; j < pages[i].Entries.Count; j++)
                    {
                        string entryName = CreateEntryName(pages[i].Entries[j], j, entryLookup);

                        TranslationFileEntry? fileEntry = entries.FirstOrDefault(x => x.PageName == pageName && x.Name == entryName);
                        if (fileEntry is null)
                            continue;

                        IGamePluginState? gameState = gamePlugin?.CreatePluginState(_stateInfo.FilePath, pages[i].Entries.AsReadOnly(), _stateInfo.FileManager);

                        ICharacterDeserializer deserializer = gameState?.TextProcessing?.Deserializer ?? new CharacterDeserializer();
                        ICharacterComposer composer = gameState?.TextProcessing?.Composer ?? new CharacterComposer();

                        IList<CharacterData> deserializedCharacters = deserializer.Deserialize(fileEntry.TranslatedText);
                        byte[] textData = composer.Compose(deserializedCharacters, pages[i].Entries[j].Encoding);

                        pages[i].Entries[j].TextData = textData;
                        pages[i].Entries[j].ContentChanged = true;
                    }
                }
            }
            else
            {
                for (var i = 0; i < _textState.Texts.Count; i++)
                {
                    string entryName = CreateEntryName(_textState.Texts[i], i, entryLookup);

                    TranslationFileEntry? fileEntry = entries.FirstOrDefault(x => x.PageName is null && x.Name == entryName);
                    if (fileEntry is null)
                        continue;

                    IGamePluginState? gameState = gamePlugin?.CreatePluginState(_stateInfo.FilePath, _textState.Texts, _stateInfo.FileManager);

                    ICharacterDeserializer deserializer = gameState?.TextProcessing?.Deserializer ?? new CharacterDeserializer();
                    ICharacterComposer composer = gameState?.TextProcessing?.Composer ?? new CharacterComposer();

                    IList<CharacterData> deserializedCharacters = deserializer.Deserialize(fileEntry.TranslatedText);
                    byte[] textData = composer.Compose(deserializedCharacters, _textState.Texts[i].Encoding);

                    _textState.Texts[i].TextData = textData;
                    _textState.Texts[i].ContentChanged = true;
                }
            }

            Console.WriteLine("Injected successfully.");
        }

        private static bool TryParseGuidArgument(string? pluginIdArgument, out Guid pluginId)
        {
            pluginId = Guid.Empty;

            if (string.IsNullOrEmpty(pluginIdArgument) ||
                Guid.TryParse(pluginIdArgument, out pluginId))
                return true;

            Console.WriteLine($"'{pluginIdArgument}' is not a valid plugin ID.");
            return false;
        }

        private static string CreatePageName(TextEntryPage page, int index, Dictionary<string, int> lookup)
        {
            if (page.Name is null)
                return $"no_name_{index:00}";

            string pageName = page.Name;

            if (!lookup.TryGetValue(pageName, out int count))
                lookup[pageName] = 1;
            else
            {
                lookup[pageName]++;
                pageName += $"_{count}";
            }

            return pageName;
        }

        private string CreateEntryName(TextEntry entry, int index, Dictionary<string, int> lookup)
        {
            if (entry.Name is null)
                return $"no_name_{index:00}";

            string entryName = entry.Name;

            if (!lookup.TryGetValue(entryName, out int count))
                lookup[entryName] = 1;
            else
            {
                lookup[entryName]++;
                entryName += $"_{count}";
            }

            return entryName;
        }
    }
}
