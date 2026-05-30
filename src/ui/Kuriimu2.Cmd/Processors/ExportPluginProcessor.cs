using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.Management.Plugin;
using Konnect.Contract.Plugin.File;
using Konnect.Contract.Plugin.Game;
using Konnect.DataClasses.Management.Batch;
using Konnect.Management.Batch;
using Konnect.Management.Files;
using Konnect.Progress;
using Kuriimu2.Cmd.Models;
using Kuriimu2.Cmd.Progress;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Kuriimu2.Cmd.Processors
{
    internal class ExportPluginProcessor(ArgumentGetter args) : PluginProcessor
    {
        public async Task Export()
        {
            if (!args.HasArguments() || args.PeekArgument() is "help")
            {
                WriteExportHelpText();
                return;
            }

            var result = await ExportInternal();

            switch (result.Reason)
            {
                case ExportFailureReason.None:
                    var unsuccessfulFileResults = result.FileResults.Where(r => r.Status is not BatchFileStatus.Success).ToArray();
                    if (unsuccessfulFileResults.Length <= 0)
                    {
                        Console.WriteLine("Exported all files successfully.");
                        break;
                    }

                    Console.WriteLine("Some files could not be extracted successfully.");
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

                case ExportFailureReason.MissingInput:
                    Console.WriteLine("No file or folder to export from provided.");
                    break;

                case ExportFailureReason.InvalidInput:
                    Console.WriteLine("File or folder to export from does not exist.");
                    break;

                case ExportFailureReason.MissingOutputValue:
                    Console.WriteLine("A file or folder to export to has to be provided after using -o.");
                    break;

                case ExportFailureReason.MissingPluginIdValue:
                    Console.WriteLine("A plugin id has to be provided after using -p.");
                    break;

                case ExportFailureReason.MissingGamePluginIdValue:
                    Console.WriteLine("A game plugin id has to be provided after using -g.");
                    break;

                case ExportFailureReason.MissingDialogOptionValues:
                    Console.WriteLine("Dialog options have to be provided after using -d.");
                    break;

                case ExportFailureReason.MissingTypeValue:
                    Console.WriteLine("An output type has to be provided after using -t.");
                    break;

                case ExportFailureReason.InvalidPluginManager:
                    Console.WriteLine("Could not load plugins.");
                    break;

                case ExportFailureReason.MissingPluginId:
                    Console.WriteLine("A plugin id has to be provided with -p when exporting from a directory.");
                    break;

                case ExportFailureReason.MissingPlugin:
                    Console.WriteLine("Could not find plugin with the given id.");
                    break;
            }
        }

        private void WriteExportHelpText()
        {
            Console.WriteLine("Required parameters (in order):");
            Console.WriteLine("  File or folder to export");
            Console.WriteLine();
            Console.WriteLine("Optional parameters:");
            Console.WriteLine("  -o\tThe folder path to export to.");
            Console.WriteLine("  -s\tDecides if sub folders should also be exported.");
            Console.WriteLine("    \tOnly applies to exporting folders.");
            Console.WriteLine("  -p\tThe plugin id to export the file or folder with.");
            Console.WriteLine("    \tRequired when exporting folders.");
            Console.WriteLine("  -d\tThe plugin options to pass to a plugin when it requests user input when exporting a file.");
            Console.WriteLine("  -g\tThe game plugin id to export the file or folder with.");
            Console.WriteLine("    \tOnly applies to files exported with a text plugin.");
            Console.WriteLine("  -t\tThe type to export the file or folder as.");
            Console.WriteLine("    \tOnly applies to files exported with a text plugin.");
            Console.WriteLine("    \tValid types for text are: po, kup. Default type is 'po'.");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine($"  Export a file: {Environment.ProcessPath} export Path/To/File.bin");
            Console.WriteLine($"  Export a file with a specific plugin: {Environment.ProcessPath} export Path/To/File.bin -p 674e7573-1f0b-454f-9a8d-7826abdd00bd");
            Console.WriteLine($"  Export a file to a different output: {Environment.ProcessPath} export Path/To/File.bin -o Path/To/Output");
            Console.WriteLine($"  Export a file with plugin options: {Environment.ProcessPath} export Path/To/File.bin -d \"Option 1\" \"Option 2\"");
            Console.WriteLine($"  Export a folder: {Environment.ProcessPath} export Path/To/Folder -p 674e7573-1f0b-454f-9a8d-7826abdd00bd");
            Console.WriteLine($"  Export a folder and its sub folders: {Environment.ProcessPath} export Path/To/Folder -p 674e7573-1f0b-454f-9a8d-7826abdd00bd -s");
            Console.WriteLine($"  Export a text file with a specific game plugin: {Environment.ProcessPath} export Path/To/File.bin -g 674e7573-1f0b-454f-9a8d-7826abdd00bd");
            Console.WriteLine($"  Export a text file in a specific format: {Environment.ProcessPath} export Path/To/File.bin -t po");
        }

        private async Task<ExportResult> ExportInternal()
        {
            var result = TryParseOptions(out ExportOptions? options);
            if (result is not ExportFailureReason.None)
                return new ExportResult(result, []);

            var pluginManager = CreatePluginManager();
            if (pluginManager is null)
                return new ExportResult(ExportFailureReason.InvalidPluginManager, []);

            var fileManager = new FileManager(pluginManager);
            if (File.Exists(options!.Input))
                options.PluginId ??= await fileManager.Identify(options.Input);

            result = TryCreateBatchOptions(options, pluginManager, out var plugin, out var batchOptions);
            if (result is not ExportFailureReason.None)
                return new ExportResult(result, []);

            var batchExtractor = new BatchExtractor(fileManager, new ProgressContext(new CommandLineProgressOutput("Export", 14)));
            BatchFileResult[] batchResults = File.Exists(options.Input)
                ? await batchExtractor.Extract([options.Input], options.Output, plugin!, batchOptions!)
                : await batchExtractor.Extract(options.Input, options.Output, plugin!, batchOptions!);

            return new ExportResult(ExportFailureReason.None, batchResults);
        }

        private static ExportFailureReason TryCreateBatchOptions(ExportOptions options, IPluginManager pluginManager, out IFilePlugin? plugin, out BatchOptions? batchOptions)
        {
            plugin = null;
            batchOptions = new BatchOptions
            {
                SubDirectories = true,
                DialogOptions = options.DialogOptions
            };

            if (!options.PluginId.HasValue)
                return ExportFailureReason.MissingPluginId;

            plugin = pluginManager.GetPlugin<IFilePlugin>(options.PluginId.Value);
            if (plugin is null)
                return ExportFailureReason.MissingPlugin;

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

            return ExportFailureReason.None;
        }

        private ExportFailureReason TryParseOptions(out ExportOptions? options)
        {
            options = null;

            if (!args.HasArguments())
                return ExportFailureReason.MissingInput;

            string input = args.ReadArgument();

            if (!File.Exists(input) && !Directory.Exists(input))
                return ExportFailureReason.InvalidInput;

            options = new ExportOptions
            {
                Input = input
            };

            while (args.HasArguments())
            {
                switch (args.ReadArgument())
                {
                    case "-o":
                        if (!args.HasArguments())
                            return ExportFailureReason.MissingOutputValue;

                        options.Output = args.ReadArgument();
                        break;

                    case "-p":
                        if (!args.HasArguments() || !Guid.TryParse(args.ReadArgument(), out Guid pluginId))
                            return ExportFailureReason.MissingPluginIdValue;

                        options.PluginId = pluginId;
                        break;

                    case "-g":
                        if (!args.HasArguments() || !Guid.TryParse(args.ReadArgument(), out Guid gamePluginId))
                            return ExportFailureReason.MissingGamePluginIdValue;

                        options.GamePluginId = gamePluginId;
                        break;

                    case "-d":
                        if (!args.HasArguments())
                            return ExportFailureReason.MissingDialogOptionValues;

                        var dialogOptions = new List<string>();
                        while (args.HasArguments() && !args.PeekArgument().StartsWith('-'))
                            dialogOptions.Add(args.ReadArgument());

                        options.DialogOptions = [.. dialogOptions];
                        break;

                    case "-t":
                        if (!args.HasArguments())
                            return ExportFailureReason.MissingTypeValue;

                        options.Type = args.ReadArgument();
                        break;

                    case "-s":
                        options.SubDirectories = true;
                        break;
                }
            }

            return ExportFailureReason.None;
        }
    }
}
