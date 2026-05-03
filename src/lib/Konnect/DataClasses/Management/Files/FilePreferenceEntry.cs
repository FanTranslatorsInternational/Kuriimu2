namespace Konnect.DataClasses.Management.Files;

public record struct FilePreferenceEntry(Guid PluginId, IList<string> Options);