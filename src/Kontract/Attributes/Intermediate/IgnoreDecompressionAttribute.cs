using System;
using Kontract.Interfaces.Common;
using Kontract.Interfaces.Intermediate;

namespace Kontract.Attributes.Intermediate
{
    /// <summary>
    /// Indicates if the decompression method of an <see cref="ICompressionAdapter"/> should be ignored.
    /// </summary>
    public class IgnoreDecompressionAttribute : Attribute, IPluginMetadata { }
}
