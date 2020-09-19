using System;

namespace Kuriimu2.WinForms.MainForms.Models
{
    public class ListItem : IComparable<ListItem>
    {
        public string Text { get; }
        public object Value { get; }

        public ListItem(string text, object value)
        {
            Text = text;
            Value = value;
        }

        public override string ToString()
        {
            return Text;
        }

        public int CompareTo(ListItem rhs)
        {
            return string.Compare(Text, rhs.Text, StringComparison.Ordinal);
        }
    }
}
