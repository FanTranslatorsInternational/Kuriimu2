namespace Kompression.PatternMatch
{
    public class Match
    {
        public long Position { get; set; }
        public long Length { get; set; }
        public long Displacement { get; set; }

        public Match(long position, long displacement, long length)
        {
            Position = position;
            Displacement = displacement;
            Length = length;
        }
    }
}
