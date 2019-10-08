using System.Windows;
using System.Windows.Controls;

namespace Kuriimu2.Controls
{
    class TrackableTextBox : TextBox
    {
        public static readonly DependencyProperty CaretPositionProperty = DependencyProperty.Register("CaretPosition", typeof(int), typeof(TrackableTextBox), new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnCaretPositionChanged));

        public int CaretPosition
        {
            get => (int)GetValue(CaretPositionProperty);
            set => SetValue(CaretPositionProperty, value);
        }

        public TrackableTextBox()
        {
            SelectionChanged += (s, e) => CaretPosition = CaretIndex;
        }

        private static void OnCaretPositionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((TrackableTextBox)d).CaretIndex = (int)e.NewValue;
    }
}
