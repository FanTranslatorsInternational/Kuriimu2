using System.Windows.Controls;
using System.Windows.Input;
using Kuriimu2.ViewModels;

namespace Kuriimu2.Views
{
    /// <summary>
    /// Interaction logic for ImageEditorView.xaml
    /// </summary>
    public partial class ImageEditorView : UserControl
    {
        public ImageEditorView()
        {
            InitializeComponent();
        }

        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            e.Handled = true;
            var vm = DataContext as ImageEditorViewModel;
            vm.MouseWheel(e);
        }
    }
}
