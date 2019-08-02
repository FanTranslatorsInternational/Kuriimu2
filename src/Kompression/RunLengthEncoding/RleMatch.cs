namespace Kompression.RunLengthEncoding
{
    public class RleMatch
    {
        public int Position { get; }
        public int Length { get; }
        public byte Value { get; }

        public RleMatch(byte value, int position, int length)
        {
            Value = value;
            Position = position;
            Length = length;
        }
    }
}
