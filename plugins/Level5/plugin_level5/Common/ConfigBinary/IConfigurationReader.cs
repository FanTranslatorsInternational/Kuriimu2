using plugin_level5.Common.ConfigBinary.Models;

namespace plugin_level5.Common.ConfigBinary
{
    public interface IConfigurationReader<TConfigEntry>
    {
        Configuration<TConfigEntry> Read(Stream input);
        Configuration<TConfigEntry> Read(Stream input, StringEncoding encoding);
    }
}
