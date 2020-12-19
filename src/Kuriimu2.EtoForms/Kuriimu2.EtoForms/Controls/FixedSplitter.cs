using System;
using Eto.Forms;

namespace Kuriimu2.EtoForms.Controls
{
    public class FixedSplitter : Splitter
    {
        private bool _isChanged;

        public new int Position { get; }

        public FixedSplitter(int position)
        {
            Position = position;
            PositionChanged += FixedSplitter_PositionChanged;

            base.Position = position;
            _isChanged = false;
        }

        private void FixedSplitter_PositionChanged(object sender, EventArgs e)
        {
            if (_isChanged)
            {
                _isChanged = false;
                return;
            }

            _isChanged = true;

            base.Position = Position;
        }
    }
}
