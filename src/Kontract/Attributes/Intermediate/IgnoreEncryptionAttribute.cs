using System;
using Kontract.Interfaces.Common;

namespace Kontract.Attributes.Intermediate
{
    /// <summary>
    /// Indicates if the encryption method of an ICipherAdapter should be ignored
    /// </summary>
    public class IgnoreEncryptionAttribute : Attribute, IPluginMetadata { }
}
