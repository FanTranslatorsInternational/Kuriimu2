using Eto.Forms;
using Kuriimu2.EtoForms.Controls;

namespace Kuriimu2.EtoForms.Forms.Formats
{
    partial class HexForm:Panel
    {
        private HexBox hexBox;

        private void InitializeComponent()
        {
            hexBox = new HexBox();
            Content = hexBox;
        }
    }
}
