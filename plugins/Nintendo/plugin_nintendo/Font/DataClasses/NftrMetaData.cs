using plugin_nintendo.Font.DataClasses.Common;

namespace plugin_nintendo.Font.DataClasses
{
    class NftrMetaData
    {
        public ushort Version { get; set; }
        public byte Type { get; set; }
        public byte Encoding { get; set; }
        public CwdhEntry DefaultWidths { get; set; }
        public byte LineFeed { get; set; }
        public byte Baseline { get; set; }
        public byte Width { get; set; }
        public byte Height { get; set; }
        public byte BearingX { get; set; }
        public byte BearingY { get; set; }
        public bool HasExtendedData { get; set; }
    }
}
