namespace Kontract.Kanvas.Configuration
{
    public delegate int CreatePaddedSizeDimension(int dimension);

    public interface IPadSizeDimensionConfiguration
    {
        IPadSizeOptions To(CreatePaddedSizeDimension func);

        IPadSizeOptions ToPowerOfTwo();

        IPadSizeOptions ToMultiple(int multiple);
    }
}
