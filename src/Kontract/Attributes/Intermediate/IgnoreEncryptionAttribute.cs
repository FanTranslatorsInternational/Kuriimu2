using System;
using Kontract.Interfaces.Plugins.State.Common;
using Kontract.Interfaces.Plugins.State.Intermediate;

namespace Kontract.Attributes.Intermediate
{
    /// <summary>
    /// Indicates if the encryption method of an <see cref="ICipherAdapter"/> should be ignored.
    /// </summary>
    public class IgnoreEncryptionAttribute : Attribute, IPluginMetadata { }
}
