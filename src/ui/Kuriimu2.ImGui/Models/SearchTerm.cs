using System;
using ImGui.Forms.Controls.Text;

namespace Kuriimu2.ImGui.Models
{
    internal class SearchTerm
    {
        private string _text = string.Empty;

        private readonly TextBox _searchTextBox;

        public event EventHandler? TextChanged;

        public SearchTerm(TextBox searchTextBox)
        {
            _searchTextBox = searchTextBox;

            searchTextBox.TextChanged += SearchTextBox_TextChanged;
        }

        public string Get()
        {
            return string.IsNullOrEmpty(_text) ? "*" : _text;
        }

        public void Clear()
        {
            _searchTextBox.Text = string.Empty;
        }

        private void SearchTextBox_TextChanged(object? sender, EventArgs e)
        {
            var hasChanged = _text != _searchTextBox.Text;
            _text = _searchTextBox.Text ?? string.Empty;

            if (hasChanged)
                OnTextChanged();
        }

        private void OnTextChanged()
        {
            TextChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
