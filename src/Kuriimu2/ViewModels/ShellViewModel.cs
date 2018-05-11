using Caliburn.Micro;

namespace Kuriimu.ViewModels
{
    public sealed class ShellViewModel : Conductor<IScreen>.Collection.OneActive
    {
        public ShellViewModel()
        {
            DisplayName = "Kuriimu";
        }

        public void TextEditor1Button()
        {
            ActivateItem(new TextEditor1ViewModel());
        }

        public void TextEditor2Button()
        {
            ActivateItem(new TextEditor2ViewModel());
        }

        public void CloseTab(Screen tab)
        {
            tab.TryClose();
        }
    }
}
