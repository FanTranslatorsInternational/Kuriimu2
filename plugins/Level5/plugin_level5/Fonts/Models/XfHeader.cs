using Komponent.IO.Attributes;

namespace plugin_level5.Fonts.Models
{
    public class XfHeader
    {
        [FixedLength(8)]
        public string magic;
        public int version;
        public short largeCharHeight;
        public short smallCharHeight;
        public short largeEscapeCharacter;
        public short smallEscapeCharacter;
        public long zero0;

        public short table0Offset;
        public short table0EntryCount;
        public short table1Offset;
        public short table1EntryCount;
        public short table2Offset;
        public short table2EntryCount;
    }
}
