using plugin_level5.Common.ConfigBinary.Models;

namespace plugin_level5.Common.ConfigBinary
{
    public interface IConfigurationWriter<TConfigEntry>
    {
        Stream Write(Configuration<TConfigEntry> config, Stream output);
    }
}
