using Kontract.Interfaces.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kontract.Attributes.Intermediate
{
    /// <summary>
    /// Indicates if the decryption method of an ICipherAdapter should be ignored
    /// </summary>
    public class IgnoreDecryptionAttribute : Attribute, IPluginMetadata
    {
    }
}
