using System.Windows.Forms;

namespace Kuriimu2.WinForms.Extensions
{
    static class LinqExtensions
    {
        public static int IndexByName(this ToolStripItemCollection collection, string name)
        {
            var index = -1;

            for (var i = 0; i < collection.Count; i++)
                if (collection[i].Name == name)
                    index = i;

            return index;
        }
    }
}
