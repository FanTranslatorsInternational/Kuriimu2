using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.Management.Plugin;
using Konnect.Contract.Plugin.File;
using Konnect.Contract.Plugin.Game;
using Konnect.DataClasses.Management.Batch;
using Konnect.Management.Batch;
using Konnect.Management.Files;
using Konnect.Progress;
using Kuriimu2.Cmd.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Kuriimu2.Cmd.Progress;

namespace Kuriimu2.Cmd.Processors
{
    internal class ImportPluginProcessor(ArgumentGetter args) : PluginProcessor
    {
        public async Task Import()
        {
            if (!args.HasArguments() || args.PeekArgument() is "help")
            {
                WriteImportHelpText();
                return;
            }

            var result = await ImportInternal();

            switch (result.Reason)
            {
                case ImportFailureReason.None:
                    var unsuccessfulFileResults = result.FileResults.Where(r => r.Status is not BatchFileStatus.Success).ToArray();
                    if (unsuccessfulFileResults.Length <= 0)
                    {
                        Console.WriteLine("Imported to all files successfully.");
                        break;
                    }

                    Console.WriteLine("Some files could not be imported to successfully.");
                    foreach (var fileResult in unsuccessfulFileResults)
                    {
                        Console.Write($"{fileResult.FilePath}: ");
                        switch (fileResult.Status)
                        {
                            case BatchFileStatus.NoOptions:
                                Console.WriteLine("Requires dialog options.");
                                break;

                            case BatchFileStatus.Error:
                                Console.WriteLine("Could not be loaded.");
                                break;
                        }
                    }
                    break;

                case ImportFailureReason.MissingInput:
                    Console.WriteLine("No file or folder to import to provided.");
                    break;

                case ImportFailureReason.InvalidInput:
                    Console.WriteLine("File or folder to import to does not exist.");
                    break;

                case ImportFailureReason.MissingOutputValue:
                    Console.WriteLine("A file or folder to import from has to be provided after using -o.");
                    break;

                case ImportFailureReason.MissingPluginIdValue:
                    Console.WriteLine("A plugin id has to be provided after using -p.");
                    break;

                case ImportFailureReason.MissingGamePluginIdValue:
                    Console.WriteLine("A game plugin id has to be provided after using -g.");
                    break;

                case ImportFailureReason.MissingDialogOptionValues:
                    Console.WriteLine("Dialog options have to be provided after using -d.");
                    break;

                case ImportFailureReason.MissingTypeValue:
                    Console.WriteLine("An output type has to be provided after using -t.");
                    break;

                case ImportFailureReason.InvalidPluginManager:
                    Console.WriteLine("Could not load plugins.");
                    break;

                case ImportFailureReason.MissingPluginId:
                    Console.WriteLine("A plugin id has to be provided with -p when importing to a directory.");
                    break;

                case ImportFailureReason.MissingPlugin:
                    Console.WriteLine("Could not find plugin with the given id.");
                    break;
            }
        }

        private void WriteImportHelpText()
        {
            Console.WriteLine("Required parameters (in order):");
            Console.WriteLine("  File or folder to import to");
            Console.WriteLine();
            Console.WriteLine("Optional parameters:");
            Console.WriteLine("  -o\tThe folder path to import from.");
            Console.WriteLine("  -s\tDecides if sub folders should also be imported to.");
            Console.WriteLine("    \tOnly applies to importing to folders.");
            Console.WriteLine("  -p\tThe plugin id to import to the file or folder.");
            Console.WriteLine("    \tRequired when importing folders.");
            Console.WriteLine("  -d\tThe plugin options to pass to a plugin when it requests user input when importing a file.");
            Console.WriteLine("  -g\tThe game plugin id to import to the file or folder.");
            Console.WriteLine("    \tOnly applies to files imported to by a text plugin.");
            Console.WriteLine("  -t\tThe type to import the file or folder from.");
            Console.WriteLine("    \tOnly applies to files imported to by a text plugin.");
            Console.WriteLine("    \tValid types for text are: po, kup. Default type is 'po'.");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine($"  Import to a file: {Environment.ProcessPath} import Path/To/File.bin");
            Console.WriteLine($"  Import to a file with a specific plugin: {Environment.ProcessPath} import Path/To/File.bin -p 674e7573-1f0b-454f-9a8d-7826abdd00bd");
            Console.WriteLine($"  Import to a file from a different input: {Environment.ProcessPath} import Path/To/File.bin -o Path/To/Input");
            Console.WriteLine($"  Import to a file with plugin options: {Environment.ProcessPath} import Path/To/File.bin -d \"Option 1\" \"Option 2\"");
            Console.WriteLine($"  Import to a folder: {Environment.ProcessPath} import Path/To/Folder -p 674e7573-1f0b-454f-9a8d-7826abdd00bd");
            Console.WriteLine($"  Import to a folder and its sub folders: {Environment.ProcessPath} import Path/To/Folder -p 674e7573-1f0b-454f-9a8d-7826abdd00bd -s");
            Console.WriteLine($"  Import to a text file with a specific game plugin: {Environment.ProcessPath} import Path/To/File.bin -g 674e7573-1f0b-454f-9a8d-7826abdd00bd");
            Console.WriteLine($"  Import to a text file from a specific format: {Environment.ProcessPath} import Path/To/File.bin -t po");
        }

        private async Task<ImportResult> ImportInternal()
        {
            var result = TryParseOptions(out ImportOptions? options);
            if (result is not ImportFailureReason.None)
                return new ImportResult(result, []);

            var pluginManager = CreatePluginManager();
            if (pluginManager is null)
                return new ImportResult(ImportFailureReason.InvalidPluginManager, []);

            var fileManager = new FileManager(pluginManager);
            if (File.Exists(options!.Input))
                options.PluginId ??= await fileManager.Identify(options.Input);

            result = TryCreateBatchOptions(options, pluginManager, out var plugin, out var batchOptions);
            if (result is not ImportFailureReason.None)
                return new ImportResult(result, []);

            var batchInjector = new BatchInjector(fileManager, new ProgressContext(new CommandLineProgressOutput("Import", 14))) { ReuseDialogOptions = true };
            BatchFileResult[] batchResults = File.Exists(options.Input)
                ? await batchInjector.Inject([options.Input], options.Output, plugin!, batchOptions!)
                : await batchInjector.Inject(options.Input, options.Output, plugin!, batchOptions!);

            return new ImportResult(ImportFailureReason.None, batchResults);
        }

        private static ImportFailureReason TryCreateBatchOptions(ImportOptions options, IPluginManager pluginManager, out IFilePlugin? plugin, out BatchOptions? batchOptions)
        {
            plugin = null;
            batchOptions = new BatchOptions
            {
                SubDirectories = true,
                DialogOptions = options.DialogOptions
            };

            if (!options.PluginId.HasValue)
                return ImportFailureReason.MissingPluginId;

            plugin = pluginManager.GetPlugin<IFilePlugin>(options.PluginId.Value);
            if (plugin is null)
                return ImportFailureReason.MissingPlugin;

            if (plugin.PluginType is PluginType.Text)
            {
                if (options.Type is null || !Enum.TryParse(options.Type, true, out TextFormat textFormat))
                    textFormat = TextFormat.Po;

                var gamePlugin = options.GamePluginId.HasValue ? pluginManager.GetPlugin<IGamePlugin>(options.GamePluginId.Value) : null;

                batchOptions.TextOptions = new BatchTextOptions
                {
                    Format = textFormat,
                    Preview = gamePlugin
                };
            }

            return ImportFailureReason.None;
        }

        private ImportFailureReason TryParseOptions(out ImportOptions? options)
        {
            options = null;

            if (!args.HasArguments())
                return ImportFailureReason.MissingInput;

            string input = args.ReadArgument();

            if (!File.Exists(input) && !Directory.Exists(input))
                return ImportFailureReason.InvalidInput;

            options = new ImportOptions
            {
                Input = input
            };

            while (args.HasArguments())
            {
                switch (args.ReadArgument())
                {
                    case "-o":
                        if (!args.HasArguments())
                            return ImportFailureReason.MissingOutputValue;

                        options.Output = args.ReadArgument();
                        break;

                    case "-p":
                        if (!args.HasArguments() || !Guid.TryParse(args.ReadArgument(), out Guid pluginId))
                            return ImportFailureReason.MissingPluginIdValue;

                        options.PluginId = pluginId;
                        break;

                    case "-g":
                        if (!args.HasArguments() || !Guid.TryParse(args.ReadArgument(), out Guid gamePluginId))
                            return ImportFailureReason.MissingGamePluginIdValue;

                        options.GamePluginId = gamePluginId;
                        break;

                    case "-d":
                        if (!args.HasArguments())
                            return ImportFailureReason.MissingDialogOptionValues;

                        var dialogOptions = new List<string>();
                        while (args.HasArguments() && !args.PeekArgument().StartsWith('-'))
                            dialogOptions.Add(args.ReadArgument());

                        options.DialogOptions = [.. dialogOptions];
                        break;

                    case "-t":
                        if (!args.HasArguments())
                            return ImportFailureReason.MissingTypeValue;

                        options.Type = args.ReadArgument();
                        break;

                    case "-s":
                        options.SubDirectories = true;
                        break;
                }
            }

            return ImportFailureReason.None;
        }
    }
}
