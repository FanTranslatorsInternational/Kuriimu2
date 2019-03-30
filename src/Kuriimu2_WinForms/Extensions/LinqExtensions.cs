using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Kuriimu2_WinForms.Extensions
{
    static class LinqExtensions
    {
        public static int IndexByName(this ToolStripItemCollection collection, string name)
        {
            int index = -1;

            for (int i = 0; i < collection.Count; i++)
                if (collection[i].Name == name)
                    index = i;

            return index;
        }
    }
}
