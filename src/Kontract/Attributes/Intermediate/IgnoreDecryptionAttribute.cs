using System;
using Kontract.Interfaces.Common;

namespace Kontract.Attributes.Intermediate
{
    /// <summary>
    /// Indicates if the decryption method of an ICipherAdapter should be ignored
    /// </summary>
    public class IgnoreDecryptionAttribute : Attribute, IPluginMetadata { }
}
