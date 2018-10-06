namespace plugin_valkyria_chronicles.MTPA
{
    public sealed class MTPAHeader
    {
        public int Unk1;
        public int PointerCount;
        public int MetadataSize;
        public int MetadataCount;
    }

    public interface ITextMetadata
    {
        int ID { get; set; }
        int Offset { get; set; }
    }

    public class TextMetadata : ITextMetadata
    {
        public int _ID;
        public int _offset;

        public int ID
        {
            get => _ID;
            set => _ID = value;
        }

        public int Offset
        {
            get => _offset;
            set => _offset = value;
        }
    }

    public class TextMetadataX : ITextMetadata
    {
        public int _ID;
        public int _zero;
        public int _offset;
        public int _flags;

        public int ID
        {
            get => _ID;
            set => _ID = value;
        }

        public int Offset
        {
            get => _offset;
            set => _offset = value;
        }
    }
}
