namespace Kompression.RunLengthEncoding
{
    public class RleMatch : IMatch
    {
        public long Position { get; set; }
        public long Length { get; set; }
        public long Displacement { get; set; } = 0;
        public byte Value { get; }

        public RleMatch(byte value, int position, int length)
        {
            Value = value;
            Position = position;
            Length = length;
        }
    }
}
