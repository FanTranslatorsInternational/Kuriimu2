namespace plugin_level5.Common.ConfigBinary.Models
{
    public class EventTextConfiguration
    {
        public DateTime? LastUpdateDateTime { get; set; }
        public string? LastUpdateUser { get; set; }
        public string? LastUpdateMachine { get; set; }
        public EventText[] Texts { get; set; }

        public StringEncoding StringEncoding { get; set; }
    }
}
