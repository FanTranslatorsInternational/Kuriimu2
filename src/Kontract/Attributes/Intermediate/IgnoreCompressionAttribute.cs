using System;
using Kontract.Interfaces.Plugins.State.Common;
using Kontract.Interfaces.Plugins.State.Intermediate;

namespace Kontract.Attributes.Intermediate
{
    /// <summary>
    /// Indicates if the compression method of an <see cref="ICompressionAdapter"/> should be ignored.
    /// </summary>
    public class IgnoreCompressionAttribute : Attribute, IPluginMetadata { }
}
