using Konnect.Contract.Plugin.File;

namespace Konnect.Contract.Exceptions.Management.Files;

public class FilePluginDeprecatedException(IDeprecatedFilePlugin deprecatedPlugin)
    : Exception
{
    public IDeprecatedFilePlugin Plugin { get; } = deprecatedPlugin;
}