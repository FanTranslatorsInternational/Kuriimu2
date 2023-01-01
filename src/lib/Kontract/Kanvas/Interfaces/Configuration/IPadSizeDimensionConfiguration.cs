namespace Kontract.Kanvas.Interfaces.Configuration
{
    public delegate int CreatePaddedSizeDimension(int dimension);

    public interface IPadSizeDimensionConfiguration
    {
        IPadSizeOptions To(CreatePaddedSizeDimension func);

        IPadSizeOptions ToPowerOfTwo(int steps = 1);

        IPadSizeOptions ToMultiple(int multiple);
    }
}
