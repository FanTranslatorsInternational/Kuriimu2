using System;
using Kontract.Interfaces.Plugins.State.Common;
using Kontract.Interfaces.Plugins.State.Intermediate;

namespace Kontract.Attributes.Intermediate
{
    /// <summary>
    /// Indicates if the decompression method of an <see cref="ICompressionAdapter"/> should be ignored.
    /// </summary>
    public class IgnoreDecompressionAttribute : Attribute, IPluginMetadata { }
}
