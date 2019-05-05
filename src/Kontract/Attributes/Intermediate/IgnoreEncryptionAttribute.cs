using System;
using Kontract.Interfaces.Common;
using Kontract.Interfaces.Intermediate;

namespace Kontract.Attributes.Intermediate
{
    /// <summary>
    /// Indicates if the encryption method of an <see cref="ICipherAdapter"/> should be ignored.
    /// </summary>
    public class IgnoreEncryptionAttribute : Attribute, IPluginMetadata { }
}
