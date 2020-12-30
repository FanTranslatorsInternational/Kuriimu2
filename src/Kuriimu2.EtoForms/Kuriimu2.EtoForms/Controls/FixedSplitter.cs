using System;
using Eto.Forms;

namespace Kuriimu2.EtoForms.Controls
{
    public class FixedSplitter : Splitter
    {
        private readonly int _position;

        private bool _isChanged;

        public FixedSplitter(int position)
        {
            Position = _position = position;
            _isChanged = false;
        }

        protected override void OnPositionChanged(EventArgs e)
        {
            if (_isChanged)
            {
                _isChanged = false;
                return;
            }

            _isChanged = true;
            switch (FixedPanel)
            {
                case SplitterFixedPanel.None:
                case SplitterFixedPanel.Panel1:
                    Position = _position;
                    break;

                case SplitterFixedPanel.Panel2:
                    var newUnit = Orientation == Orientation.Horizontal ? Size.Width : Size.Height;
                    Position = newUnit - _position;
                    break;
            }
        }
    }
}
