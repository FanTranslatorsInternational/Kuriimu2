using Eto.Forms;

namespace Kuriimu2.EtoForms.Forms.Formats
{
    partial class TextForm : Panel
    {
        private StackLayout entryLayout;
        private RichTextArea sourceText;
        private RichTextArea targetText;

        private void InitializeComponent()
        {
            entryLayout=new StackLayout{Orientation=Orientation.Vertical };

            #region Content

            Content = new StackLayout
            {
                Orientation = Orientation.Vertical,
                Items =
                {
                    new Scrollable
                    {
                        Content=entryLayout
                    },
                    sourceText,
                    targetText
                }
            };

            #endregion
        }
    }
}
