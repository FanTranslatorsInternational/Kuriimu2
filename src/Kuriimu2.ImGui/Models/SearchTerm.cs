using System;
using System.Threading.Tasks;
using ImGui.Forms.Controls;

namespace Kuriimu2.ImGui.Models
{
    class SearchTerm
    {
        private const int ChangeTimer_ = 1000;

        private DateTime _changeTime;
        private Task _changeTask;
        private string _tempText;
        private string _text;

        private readonly TextBox _searchTextBox;

        public event EventHandler TextChanged;

        public SearchTerm(TextBox searchTextBox)
        {
            _searchTextBox = searchTextBox;

            searchTextBox.TextChanged += searchTextBox_TextChanged;
        }

        public string Get()
        {
            return string.IsNullOrEmpty(_text) ? "*" : _text;
        }

        public void Clear()
        {
            _searchTextBox.Text = null;
        }

        private void searchTextBox_TextChanged(object sender, EventArgs e)
        {
            _text = _searchTextBox.Text;
            OnTextChanged();

            //// Manage internal state
            //_tempText = _searchTextBox.Text;
            //_changeTime = DateTime.Now + TimeSpan.FromMilliseconds(ChangeTimer_);

            //// If a wait for the event to submit is still going
            //if (_changeTask != null && !_changeTask.IsCompleted)
            //    return;

            //// Await text changing, also reacts to intermediate changes as much as possible
            //_changeTask = Task.Run(async () => await WaitOnTime());
            //await _changeTask;
        }

        private async Task WaitOnTime()
        {
            // Wait for last change to be significantly in the past
            var delta = _changeTime - DateTime.Now;
            while (delta.Milliseconds > 0)
            {
                await Task.Delay(delta);
                delta = _changeTime - DateTime.Now;
            }

            // Send change events until temp buffer and actual text are same
            while (_text != _tempText)
            {
                _text = _tempText;
                OnTextChanged();
            }
        }

        private void OnTextChanged()
        {
            TextChanged?.Invoke(this, new EventArgs());
        }
    }
}
