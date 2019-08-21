namespace Kompression.LempelZiv
{
    public class LzMatch : IMatch
    {
        public long Position { get; set; }
        public long Displacement { get; set; }
        public long Length { get; set; }

        public LzMatch(long position, long displacement, long length)
        {
            Position = position;
            Displacement = displacement;
            Length = length;
        }
    }
}
