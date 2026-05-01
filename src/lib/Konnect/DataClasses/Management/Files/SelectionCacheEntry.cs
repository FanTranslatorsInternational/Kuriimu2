namespace Konnect.DataClasses.Management.Files;

public record struct SelectionCacheEntry(Guid PluginId, IList<string> Options);