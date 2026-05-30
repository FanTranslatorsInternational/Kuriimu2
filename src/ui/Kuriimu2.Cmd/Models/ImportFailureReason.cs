namespace Kuriimu2.Cmd.Models
{
    internal enum ImportFailureReason
    {
        None,
        MissingInput,
        InvalidInput,
        MissingOutputValue,
        MissingPluginIdValue,
        MissingGamePluginIdValue,
        MissingDialogOptionValues,
        MissingTypeValue,
        InvalidPluginManager,
        MissingPluginId,
        MissingPlugin
    }
}
