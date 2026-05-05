using Kanvas.Contract.Configuration;

namespace Kanvas.Configuration
{
    internal class SizePaddingDimensionConfigurationBuilder(
        ISizePaddingConfigurationBuilder parent,
        Action<CreatePaddedSizeDimensionDelegate> dimensionSetDelegate)
        : ISizePaddingDimensionConfigurationBuilder
    {
        public ISizePaddingConfigurationBuilder To(int dimension)
        {
            dimensionSetDelegate.Invoke(_ => dimension);
            return parent;
        }

        public ISizePaddingConfigurationBuilder To(CreatePaddedSizeDimensionDelegate dimensionDelegateDelegate)
        {
            dimensionSetDelegate.Invoke(dimensionDelegateDelegate);
            return parent;
        }

        public ISizePaddingConfigurationBuilder ToPowerOfTwo(int steps = 1)
        {
            dimensionSetDelegate.Invoke(value => SizePadding.PowerOfTwo(value, steps));
            return parent;
        }

        public ISizePaddingConfigurationBuilder ToMultiple(int multiple)
        {
            dimensionSetDelegate.Invoke(value => SizePadding.Multiple(value, multiple));
            return parent;
        }
    }
}
