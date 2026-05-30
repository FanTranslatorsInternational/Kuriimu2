using System;
using Kuriimu2.Cmd;
using Kuriimu2.Cmd.Processors;

var argumentGetter = new ArgumentGetter(args);

if (!argumentGetter.HasArguments())
{
    WriteHelpText();
    return;
}

switch (argumentGetter.ReadArgument())
{
    case "help":
        WriteHelpText();
        break;

    case "update":
        await UpdateProcessor.Update();
        break;

    case "export":
        var exportProcessor = new ExportPluginProcessor(argumentGetter);
        await exportProcessor.Export();
        break;

    case "import":
        var importProcessor = new ImportPluginProcessor(argumentGetter);
        await importProcessor.Import();
        break;

    case "list":
        var listProcessor = new ListPluginProcessor();
        listProcessor.List();
        break;
}

void WriteHelpText()
{
    Console.WriteLine("Following commands exist:");
    Console.WriteLine("  help\t\tShows this help message.");
    Console.WriteLine("  update\tUpdates this command line tool.");
    Console.WriteLine("  export\tExports a file.");
    Console.WriteLine("  import\tImports a folder to a file.");
    Console.WriteLine("  list\t\tLists all installed plugins.");
}
