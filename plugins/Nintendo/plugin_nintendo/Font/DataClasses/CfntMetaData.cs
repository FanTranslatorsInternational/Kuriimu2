namespace plugin_nintendo.Font.DataClasses
{
    class CfntMetaData
    {
        public int Version { get; set; }
        public byte Type { get; set; }
        public byte Encoding { get; set; }
        public CfntCwdhEntry DefaultWidths { get; set; }
        public byte LineFeed { get; set; }
        public byte Baseline { get; set; }
        public byte Ascent { get; set; }
        public byte Width { get; set; }
        public byte Height { get; set; }
    }
}
