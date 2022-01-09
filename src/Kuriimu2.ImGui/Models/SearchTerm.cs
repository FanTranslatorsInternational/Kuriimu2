using System;
using ImGui.Forms.Controls;

namespace Kuriimu2.ImGui.Models
{
    class SearchTerm
    {
        private readonly TextBox _searchTextBox;

        public event EventHandler TextChanged;

        public SearchTerm(TextBox searchTextBox)
        {
            _searchTextBox = searchTextBox;

            searchTextBox.TextChanged += searchTextBox_TextChanged;
        }

        public string Get()
        {
            return string.IsNullOrEmpty(_searchTextBox.Text) ? "*" : _searchTextBox.Text;
        }

        public void Clear()
        {
            _searchTextBox.Text = string.Empty;
        }

        private void searchTextBox_TextChanged(object sender, EventArgs e)
        {
            TextChanged?.Invoke(this, new EventArgs());
        }
    }
}
