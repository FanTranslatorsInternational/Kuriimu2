using ImGui.Forms;
using Kuriimu2.ImGui.Forms;
using Kuriimu2.ImGui.Resources;

var app = new Application(LocalizationResources.Instance);
var form = new MainForm();

FontResources.RegisterFonts();

form.DefaultFont = FontResources.GetFont(FontType.Application, 15);

app.Execute(form);