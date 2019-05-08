using System.Windows;
using System.Windows.Controls;

namespace Kuriimu2.AttachedProperties
{
    public class DataGridProperties
    {
        public static readonly DependencyProperty SelectingItemProperty = DependencyProperty.RegisterAttached(
            name: "SelectingItem",
            propertyType: typeof(object),
            ownerType: typeof(DataGridProperties),
            defaultMetadata: new PropertyMetadata(defaultValue: default(object), propertyChangedCallback: OnSelectingItemChanged)
        );

        private static void OnSelectingItemChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var grid = sender as DataGrid;
            if (grid?.SelectedItem == null)
                return;

            grid.Dispatcher.InvokeAsync(() =>
            {
                grid.UpdateLayout();
                if (grid.SelectedItem != null)
                    grid.ScrollIntoView(grid.SelectedItem, null);
            });
        }

        public static object GetSelectingItem(DependencyObject element)
        {
            return element.GetValue(SelectingItemProperty);
        }

        public static void SetSelectingItem(DependencyObject element, object value)
        {
            element.SetValue(SelectingItemProperty, value);
        }
    }
}
