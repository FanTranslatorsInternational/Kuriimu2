using System;
using System.IO;
using Eto.Drawing;
using Eto.Forms;
using Kuriimu2.EtoForms.Support;

namespace Kuriimu2.EtoForms.Controls
{
    public class HexBox : ScrollableEx
    {
        private static HexBoxImpl _hexBoxImpl;

        #region Properties

        public int FontSize
        {
            get => _hexBoxImpl.FontSize;
            set
            {
                _hexBoxImpl.FontSize = value;
                Invalidate();
            }
        }

        public int ByteGroup
        {
            get => _hexBoxImpl.ByteGroup;
            set
            {
                _hexBoxImpl.ByteGroup = value;
                Invalidate();
            }
        }

        public int BytesPerLine
        {
            get => _hexBoxImpl.BytesPerLine;
            set
            {
                _hexBoxImpl.BytesPerLine = value;
                Invalidate();
            }
        }

        public bool ShowLinePositions
        {
            get => _hexBoxImpl.ShowLinePositions;
            set
            {
                _hexBoxImpl.ShowLinePositions = value;
                Invalidate();
            }
        }

        public bool ShowHeader
        {
            get => _hexBoxImpl.ShowHeader;
            set
            {
                _hexBoxImpl.ShowHeader = value;
                Invalidate();
            }
        }

        #endregion

        public HexBox()
        {
            Content = _hexBoxImpl = new HexBoxImpl();
        }

        public void LoadStream(Stream input)
        {
            _hexBoxImpl.LoadStream(input);
        }
    }

    class HexBoxImpl : IScrollableContent
    {
        private const int LinePadding = 5;
        private const int BytePadding = 3;
        private const int ByteGroupPadding = 10;
        private const int PositionWidthPadding = 7;

        private Font _monoFont;
        private Font _sansFont;
        private float _monoHeight;
        private float _sansHeight;
        private int _lineHeight;

        private Stream _data;
        private int _fontSize = 10;

        private Color SideBarBackground => Color.FromArgb(0xcd, 0xf7, 0xfd);
        private Color ByteBackground1 => Color.FromArgb(0xf0, 0xfd, 0xff);

        #region Properties

        public Size Size { get; set; }

        public int FontSize { get; set; } = 12;

        public int ByteGroup { get; set; } = 4;

        public int BytesPerLine { get; set; } = 16;

        public bool ShowLinePositions { get; set; } = true;

        public bool ShowHeader { get; set; } = true;

        #endregion

        public HexBoxImpl()
        {
            UpdateFontInformation(_fontSize);
        }

        public void LoadStream(Stream input)
        {
            _data = input;

            var headerHeight = _monoFont.MeasureString("A").Height;
            var lineStartY = ShowHeader ? headerHeight : 0;

            // TODO: Calculate actual width
            var lines = input.Length / BytesPerLine + (input.Length % BytesPerLine > 0 ? 1 : 0);
            Size = new Size(0, (int)(lines * _lineHeight + lineStartY));
        }

        private int GetPositionWidth(bool isLongPosition)
        {
            return (int)GetFontWidth(_monoFont, isLongPosition ? "CCCCCCCC" : "CCCCCCCCCCCCCCCC");
        }

        #region Font methods

        private void UpdateFontInformation(float size)
        {
            _monoFont = CreateMonoFont(size);
            _sansFont = CreateSansFont(size);
            _monoHeight = GetFontHeight(_monoFont);
            _sansHeight = GetFontHeight(_sansFont);
            _lineHeight = (int)Math.Ceiling(_monoHeight + LinePadding);
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

        public void Paint(RelativeGraphics g, RectangleF clipRectangle)
        {
            var headerHeight = _monoFont.MeasureString("A").Height;
            var lineStartY = ShowHeader ? headerHeight + 5 : 0;

            // Read buffer
            var startLine = (long)((clipRectangle.Y == 0 ? 0 : clipRectangle.Y + lineStartY) / _lineHeight);
            var totalLines = (float)Math.Ceiling((clipRectangle.Height - lineStartY) / _lineHeight +
                             ((clipRectangle.Height - lineStartY) % _lineHeight > 0 ? 1 : 0));

            var dataBuffer = new byte[(int)(BytesPerLine * totalLines)];
            _data.Position = startLine * BytesPerLine;
            _data.Read(dataBuffer, 0, dataBuffer.Length);

            // Draw background colors
            var positionWidth = GetPositionWidth((startLine + (long)totalLines) * BytesPerLine < int.MaxValue) + PositionWidthPadding * 2;
            var lineStartX = ShowLinePositions ? positionWidth + 5 : 0;
            var monoCenteredY = (int)((_lineHeight - Math.Ceiling(_monoHeight)) / 2);
            var byteWidth = GetFontWidth(_monoFont, "FF") + BytePadding;

            if (ShowLinePositions)
                g.FillRectangle(SideBarBackground, clipRectangle.X, clipRectangle.Y + lineStartY, positionWidth, clipRectangle.Height - lineStartY);

            if (ShowHeader)
                g.FillRectangle(SideBarBackground, clipRectangle.X + lineStartX, clipRectangle.Y, clipRectangle.Width - lineStartX, headerHeight);

            var byteX = (float)lineStartX;
            var byteY = clipRectangle.Y + lineStartY;
            for (var i = 0; i < BytesPerLine; i++)
            {
                if (i > 0 && i % ByteGroup == 0)
                    byteX += ByteGroupPadding;

                if (i % 2 == 1)
                    g.FillRectangle(ByteBackground1, byteX, byteY, byteWidth, clipRectangle.Height);

                byteX += byteWidth;
            }

            // Draw lines
            for (var l = 0; l < totalLines; l++)
            {
                if (l >= dataBuffer.Length / BytesPerLine)
                    break;

                byteX = lineStartX;
                for (var i = 0; i < BytesPerLine; i++)
                {
                    if (i >= Math.Min(BytesPerLine, dataBuffer.Length - l * BytesPerLine))
                        break;

                    if (i > 0 && i % ByteGroup == 0)
                        byteX += ByteGroupPadding;

                    var bytePosition = new PointF(byteX, byteY + monoCenteredY);
                    g.DrawText(_monoFont, new SolidBrush(KnownColors.Black), bytePosition, $"{dataBuffer[l * BytesPerLine + i]:X2}");

                    byteX += byteWidth;
                }

                byteY += _lineHeight;
            }

            // Draw line positions
            if (ShowLinePositions)
            {
                byteY = clipRectangle.Y + lineStartY;
                for (var l = 0; l < totalLines; l++)
                {
                    var linePosition = new PointF(PositionWidthPadding, byteY + monoCenteredY);
                    g.DrawText(_monoFont, new SolidBrush(KnownColors.Black), linePosition, $"{(startLine + l) * BytesPerLine:X8}");

                    byteY += _lineHeight;
                }

                g.DrawLine(KnownColors.ControlDark, new PointF(clipRectangle.X + positionWidth, clipRectangle.Y), new PointF(clipRectangle.X + positionWidth, clipRectangle.Y + clipRectangle.Height));
            }

            // Draw header
            if (ShowHeader)
            {
                byteX = lineStartX;
                for (var i = 0; i < BytesPerLine; i++)
                {
                    if (i > 0 && i % ByteGroup == 0)
                        byteX += ByteGroupPadding;

                    var bytePosition = new PointF(byteX, clipRectangle.Y + monoCenteredY);
                    g.DrawText(_monoFont, new SolidBrush(KnownColors.Black), bytePosition, $"{i:X2}");

                    byteX += byteWidth;
                }

                g.DrawLine(KnownColors.ControlDark, new PointF(clipRectangle.X, clipRectangle.Y + (int)headerHeight), new PointF(clipRectangle.X + clipRectangle.Width, clipRectangle.Y + (int)headerHeight));
            }
        }
    }
}
