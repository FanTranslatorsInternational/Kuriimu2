using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kuriimu2_WinForms.MainForms.Models
{
    class ComboBoxElement
    {
        public object Value { get; }
        public string Name { get; }

        public ComboBoxElement(object value, string name)
        {
            Value = value;
            Name = name;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
