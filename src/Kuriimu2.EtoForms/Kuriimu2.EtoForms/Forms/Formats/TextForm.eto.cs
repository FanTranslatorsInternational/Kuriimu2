using Eto.Forms;
using Kuriimu2.EtoForms.Controls;

namespace Kuriimu2.EtoForms.Forms.Formats
{
    partial class TextForm : Panel
    {
        private CatEntryList entryList;
        private RichTextArea sourceText;
        private RichTextArea targetText;
        private Label withoutCodeLabel;

        private void InitializeComponent()
        {
            entryList = new CatEntryList(_formInfo.PluginState){ ExpandContentWidth=true,};
            sourceText = new RichTextArea{ReadOnly=true};
            targetText = new RichTextArea();
            withoutCodeLabel = new Label();

            var textContent = new TableLayout
            {
                Spacing = new Eto.Drawing.Size(2, 2),
                Padding = new Eto.Drawing.Padding(2, 0, 0, 0),
                Rows =
                {
                    new TableRow
                    {
                        ScaleHeight = true,
                        Cells = { sourceText }
                    },
                    new TableRow
                    {
                        ScaleHeight = true,
                        Cells = { targetText }
                    },
                    new TableRow
                    {
                        ScaleHeight = true,
                        Cells = { withoutCodeLabel }
                    }
                }
            };

            Content = new TableLayout
            {
                Rows =
                {
                    new TableRow
                    {
                        Cells =
                        {
                            new TableCell(entryList){ScaleWidth=true},
                            new TableCell(textContent){ScaleWidth=true}
                        }
                    }
                }
            };
        }
    }
}
