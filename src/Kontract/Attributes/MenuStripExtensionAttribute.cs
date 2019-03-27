using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kontract.Attributes
{
    public class MenuStripExtensionAttribute:Attribute
    {
        public string[] Strips { get; }

        public MenuStripExtensionAttribute(params string[] strips)
        {
            Strips = strips;
        }
    }
}
