namespace plugin_level5.Common.ConfigBinary.Models
{
    public class Configuration<TConfigEntry>
    {
        public TConfigEntry[] Entries { get; set; }
        public StringEncoding Encoding { get; set; }
    }
}
