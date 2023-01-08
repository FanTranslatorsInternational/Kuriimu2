using System.Text.RegularExpressions;
using Kontract;
using Kontract.Interfaces.Plugins.Entry;
using Kontract.Models.FileSystem;

namespace Kore.Models.Managers.Files.Support
{
    internal class PhysicalLoadInfo
    {
        public UPath FilePath { get; }

        public IFilePlugin Plugin { get; }

        public PhysicalLoadInfo(UPath filePath)
        {
            ContractAssertions.IsNotNull(filePath, nameof(filePath));

            var internalPath = ReplaceWindowsDrive(filePath);
            FilePath = internalPath;
        }

        public PhysicalLoadInfo(UPath filePath, IFilePlugin plugin) :
            this(filePath)
        {
            ContractAssertions.IsNotNull(plugin, nameof(plugin));

            Plugin = plugin;
        }

        private static UPath ReplaceWindowsDrive(UPath path)
        {
            var driveRegex = new Regex(@"^[a-zA-Z]:[/\\]");
            if (!driveRegex.IsMatch(path.FullName))
                return path;

            var driveLetter = path.FullName[0];
            return new UPath(driveRegex.Replace(path.FullName, $"/mnt/{char.ToLower(driveLetter)}/"));
        }
    }
}
