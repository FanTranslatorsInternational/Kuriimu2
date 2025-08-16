namespace plugin_level5.Common.ConfigBinary.Models
{
    public class RawConfigurationEntry
    {
        public uint Hash { get; set; }
        public ConfigurationEntryValue[] Values { get; set; }
    }
}
