using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Kuriimu2.Wpf.Controls
{
    public class ScrollDragger
    {
        private readonly ScrollViewer _scrollViewer;
        private readonly UIElement _content;
        private Point _scrollMousePoint;
        private double _hOff = 1;

        public ScrollDragger(UIElement content, ScrollViewer scrollViewer)
        {
            _scrollViewer = scrollViewer;
            _content = content;
            content.MouseLeftButtonDown += scrollViewer_MouseLeftButtonDown;
            content.PreviewMouseMove += scrollViewer_PreviewMouseMove;
            content.PreviewMouseLeftButtonUp += scrollViewer_PreviewMouseLeftButtonUp;
        }

        private void scrollViewer_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _content.CaptureMouse();
            _scrollMousePoint = e.GetPosition(_scrollViewer);
            _hOff = _scrollViewer.VerticalOffset;
        }

        private void scrollViewer_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (_content.IsMouseCaptured)
            {
                var newOffset = _hOff + (_scrollMousePoint.Y - e.GetPosition(_scrollViewer).Y);
                _scrollViewer.ScrollToVerticalOffset(newOffset);
            }
        }

        private void scrollViewer_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _content.ReleaseMouseCapture();
        } 
    }
}
