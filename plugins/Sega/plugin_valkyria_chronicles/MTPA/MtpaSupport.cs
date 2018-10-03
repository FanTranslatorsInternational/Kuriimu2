namespace plugin_valkyria_chronicles.MTPA
{
    public sealed class MTPAPacketHeader
    {
        public int Unk1;
        public int PointerCount;
        public int DataSize;
        public int DataCount;
    }

    public class TextMetadata
    {
        public int ID;
        public int Offset;
    }

    public class TextMetadataX
    {
        public int ID;
        public int Zero;
        public int Offset;
        public int Flag;
    }
}
