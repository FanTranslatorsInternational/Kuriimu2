using Kanvas.Contract.Configuration;
using Kanvas.DataClasses.Configuration;

namespace Kanvas.Configuration
{
    internal class ColorShaderConfigurationBuilder(
        IImageConfigurationBuilder parent,
        ColorShaderConfigurationOptions options)
        : IColorShaderConfigurationBuilder
    {
        public IImageConfigurationBuilder With(CreateColorShaderDelegate shaderDelegate)
        {
            options.ColorShaderDelegate = shaderDelegate;
            return parent;
        }
    }
}
