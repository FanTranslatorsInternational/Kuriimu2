using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kontract.Attributes
{
    /// <summary>
    /// Marker attribute that makes fields invisible to the property editor.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class FormFieldIgnoreAttribute : Attribute { }
}
