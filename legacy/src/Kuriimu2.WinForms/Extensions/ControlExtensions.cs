using System.Windows.Forms;

namespace Kuriimu2.WinForms.Extensions
{
    static class ControlExtensions
    {
        public static Control GetActiveControl(this IContainerControl container)
        {
            Control control = null;

            while (container != null)
            {
                control = container.ActiveControl;
                container = control as IContainerControl;
            }

            return control;
        }
    }
}
