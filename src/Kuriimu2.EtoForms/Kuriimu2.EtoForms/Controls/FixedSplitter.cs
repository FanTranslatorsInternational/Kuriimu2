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
            Position = _position;
        }
    }
}
