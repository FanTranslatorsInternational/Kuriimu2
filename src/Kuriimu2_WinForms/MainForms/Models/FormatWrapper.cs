using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kuriimu2_WinForms.MainForms.Models
{
    internal class FormatWrapper
    {
        public object Value { get; }
        public string Name { get; }

        public FormatWrapper(object value, string name)
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
