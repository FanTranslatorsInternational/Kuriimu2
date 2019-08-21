namespace Kompression
{
    public interface IMatch
    {
        long Position { get; set; }
        long Length { get; set; }
        long Displacement { get; set; }
    }
}
