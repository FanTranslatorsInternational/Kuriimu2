namespace Kompression.LempelZiv
{
    public class LzMatch
    {
        public long Position { get; private set; }
        public long Displacement { get; }
        public int Length { get; }

        public LzMatch(long position, long displacement, int length)
        {
            Position = position;
            Displacement = displacement;
            Length = length;
        }

        public void SetPosition(long position)
        {
            Position = position;
        }
    }
}
