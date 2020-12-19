using System;
using System.IO;
using Eto.Drawing;
using Eto.Forms;
using Kuriimu2.EtoForms.Support;

namespace Kuriimu2.EtoForms.Controls
{
    class HexBox : Drawable
    {
        private const int LinePadding = 5;
        private const int BytePadding = 3;

        private Font _monoFont;
        private Font _sansFont;
        private float _monoHeight;
        private float _sansHeight;

        private Stream _data;
        private int _fontSize = 8;
        private int _bytesPerLine = 16;
        private bool _showHeader = true;
        private bool _showLinePositions = true;

        #region Properties

        public int FontSize
        {
            get => _fontSize;
            set
            {
                _fontSize = value;
                UpdateFontInformation(value);

                Invalidate();
            }
        }

        public int BytesPerLine
        {
            get => _bytesPerLine;
            set
            {
                _bytesPerLine = value;
                Invalidate();
            }
        }

        public bool ShowLinePositions
        {
            get => _showLinePositions;
            set
            {
                _showLinePositions = value;
                Invalidate();
            }
        }

        public bool ShowHeader
        {
            get => _showHeader;
            set
            {
                _showHeader = value;
                Invalidate();
            }
        }

        #endregion

        public HexBox()
        {
            UpdateFontInformation(_fontSize);
        }

        public void LoadStream(Stream input)
        {
            _data = input;

            var lines = input.Length / BytesPerLine + (input.Length % BytesPerLine > 0 ? 1 : 0);
            Size = new Size(Size.Width, (int)(lines * ((int)Math.Max(_monoHeight, _sansHeight) + LinePadding)));
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var lineHeight = (int)Math.Ceiling(Math.Max(_monoHeight, _sansHeight)) + LinePadding;

            // Read buffer
            var startLine = (long)(e.ClipRectangle.Y / lineHeight);
            var totalLines = e.ClipRectangle.Height / lineHeight +
                             (e.ClipRectangle.Height % lineHeight > 0 ? 1 : 0);

            var dataBuffer = new byte[(int)(BytesPerLine * totalLines)];
            _data.Position = startLine * BytesPerLine;
            _data.Read(dataBuffer, 0, dataBuffer.Length);

            // Draw line positions
            var positionWidth = GetPositionWidth((startLine + (long)totalLines) * BytesPerLine < int.MaxValue);
            if (ShowLinePositions)
            {
                var sansCenteredY = (int)((lineHeight - Math.Ceiling(_sansHeight)) / 2);
                for (var l = 0; l < totalLines; l++)
                {
                    var linePosition = new PointF(0, (startLine + l) * lineHeight + sansCenteredY);
                    e.Graphics.DrawText(_sansFont, new SolidBrush(KnownColors.Black), linePosition, $"{(startLine + l) * BytesPerLine:X8}");
                }

                e.Graphics.DrawLine(KnownColors.ControlDark, new PointF(positionWidth, startLine * lineHeight), new PointF(positionWidth, (startLine + totalLines) * lineHeight));
            }

            // Draw lines
            var lineStart = ShowLinePositions ? positionWidth + 5 : 0;
            var byteCenteredY = (int)((lineHeight - Math.Ceiling(_monoHeight)) / 2);
            var byteWidth = GetFontWidth(_monoFont, "FF") + BytePadding;
            for (var l = 0; l < totalLines; l++)
            {
                if (l >= dataBuffer.Length / BytesPerLine)
                    break;

                for (var i = 0; i < BytesPerLine; i++)
                {
                    if (i >= Math.Min(BytesPerLine, dataBuffer.Length - l * BytesPerLine))
                        break;

                    var currentLine = startLine + l;
                    var bytePosition = new PointF(lineStart + i * byteWidth, currentLine * lineHeight + byteCenteredY);
                    e.Graphics.DrawText(_monoFont, new SolidBrush(KnownColors.Black), bytePosition, $"{dataBuffer[l * BytesPerLine + i]:X2}");
                }
            }
        }

        private int GetPositionWidth(bool isLongPosition)
        {
            return (int)GetFontWidth(_sansFont, isLongPosition ? "FFFFFFFF" : "FFFFFFFFFFFFFFFF");
        }

        #region Font methods

        private void UpdateFontInformation(float size)
        {
            _monoFont = CreateMonoFont(size);
            _sansFont = CreateSansFont(size);
            _monoHeight = GetFontHeight(_monoFont);
            _sansHeight = GetFontHeight(_sansFont);
        }

        private static float GetFontHeight(Font font)
        {
            return font.MeasureString("A").Height;
        }

        private static float GetFontWidth(Font font, string text)
        {
            return font.MeasureString(text).Width;
        }

        private Font CreateMonoFont(float size)
        {
            return new Font(FontFamilies.Monospace, size);
        }

        private Font CreateSansFont(float size)
        {
            return new Font(FontFamilies.Sans, size);
        }

        #endregion
    }
}
