using System.Windows;
using System.Windows.Input;
using Kuriimu2.Dialogs.ViewModels;

namespace Kuriimu2.Dialogs.Views
{
    /// <summary>
    /// Interaction logic for EncodeImageView.xaml
    /// </summary>
    public partial class EncodeImageView : Window
    {
        public EncodeImageView()
        {
            InitializeComponent();
        }

        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            e.Handled = true;
            var vm = DataContext as EncodeImageViewModel;
            vm.MouseWheel(e);
        }

        private void ScrollChanged(object sender, System.Windows.Controls.ScrollChangedEventArgs e)
        {
            if (sender == SourceImageScroller)
            {
                OutputImageScroller.ScrollToVerticalOffset(e.VerticalOffset);
                OutputImageScroller.ScrollToHorizontalOffset(e.HorizontalOffset);
            }
            else
            {
                SourceImageScroller.ScrollToVerticalOffset(e.VerticalOffset);
                SourceImageScroller.ScrollToHorizontalOffset(e.HorizontalOffset);
            }
        }
    }
}
