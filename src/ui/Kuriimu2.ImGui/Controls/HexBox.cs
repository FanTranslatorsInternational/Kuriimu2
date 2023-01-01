using System;
using System.IO;
using System.Numerics;
using ImGui.Forms.Controls;
using ImGui.Forms.Controls.Base;
using ImGui.Forms.Controls.Layouts;
using ImGui.Forms.Models;
using Veldrid;

namespace Kuriimu2.ImGui.Controls
{
    class HexBox : Component
    {
        #region Constants

        private const int BytesPerLine_ = 16;
        private const int GroupSize_ = 4;
        private const int LineSpace_ = 3;
        private const int GroupSpace_ = 10;

        #endregion

        #region Layout components

        private TableLayout _hexBox;
        private TextBox _positionTextBox;
        private PositionLineListing _positionsLineListing;
        private PositionListing _positionsListing;

        private ByteContainer _byteContainer;
        private StringContainer _stringContainer;

        #endregion

        private int _bytesPerLine;
        private int _groupSize;
        private int _lineSpace;
        private int _groupSpace;

        #region Properties

        public Stream Data
        {
            get => _byteContainer.Data;
            set
            {
                _byteContainer.Data = value;
                _stringContainer.Data = value;

                _positionTextBox.MaxCharacters = (uint)(value?.Length > int.MaxValue ? 16 : 8);

                _positionsLineListing.MaxCharacters = (int)_positionTextBox.MaxCharacters;
                _positionsLineListing.CurrentLine = 0;

                Position = 0;
            }
        }

        public long Position
        {
            get => _byteContainer.Position;
            set
            {
                _byteContainer.Position = value;
                _stringContainer.Position = value;
            }
        }

        public int BytesPerLine
        {
            get => _bytesPerLine;
            set
            {
                _bytesPerLine = value;
                _positionsListing.BytesPerLine = value;
                _positionsLineListing.BytesPerLine = value;
                _byteContainer.BytesPerLine = value;
                _stringContainer.BytesPerLine = value;

                _positionsLineListing.CurrentLine = (int)(Position / value);
            }
        }

        public int GroupSize
        {
            get => _groupSize;
            set
            {
                _groupSize = value;
                _positionsListing.GroupSize = value;
                _byteContainer.GroupSize = value;
            }
        }

        public int LineSpace
        {
            get => _lineSpace;
            set
            {
                _lineSpace = value;
                _positionsLineListing.LineSpace = value;
                _byteContainer.LineSpace = value;
                _stringContainer.LineSpacing = value;
            }
        }

        public int GroupSpace
        {
            get => _groupSpace;
            set
            {
                _groupSpace = value;
                _positionsListing.GroupSpace = value;
                _byteContainer.GroupSpace = value;
            }
        }

        #endregion

        #region Events

        public event EventHandler PositionChanged;

        #endregion

        public HexBox()
        {
            SetupHexLayout();
        }

        public override Size GetSize()
        {
            return Size.Parent;
        }

        protected override void UpdateInternal(Rectangle contentRect)
        {
            _hexBox.Update(contentRect);
        }

        private void SetupHexLayout()
        {
            _positionTextBox = new TextBox { MaxCharacters = 8, Width = (int)ImGuiNET.ImGui.CalcTextSize(new string('A', 8)).X};
            _positionsLineListing = new PositionLineListing();
            _positionsListing = new PositionListing();
            _byteContainer = new ByteContainer();
            _stringContainer = new StringContainer();

            _hexBox = new TableLayout
            {
                Spacing = new Vector2(5,5),
                Rows =
                {
                    new TableRow
                    {
                        Cells =
                        {
                            new TableCell(_positionTextBox){HorizontalAlignment = HorizontalAlignment.Left},
                            _positionsListing
                        }
                    },
                    new TableRow
                    {
                        Cells =
                        {
                            _positionsLineListing,
                            _byteContainer,
                            _stringContainer
                        }
                    }
                }
            };

            BytesPerLine = BytesPerLine_;
            GroupSize = GroupSize_;
            LineSpace = LineSpace_;
            GroupSpace = GroupSpace_;

            _byteContainer.PositionChanged += (s, e) => OnPositionChanged();
            _byteContainer.MouseScrolled += (s, e) =>
            {
                _positionsLineListing.CurrentLine = e.ScrollLine;
                _stringContainer.CurrentLine = e.ScrollLine;
            };
        }

        #region Event Invokers

        private void OnPositionChanged()
        {
            PositionChanged?.Invoke(this, new EventArgs());
        }

        #endregion

        #region Custom classes

        class PositionLineListing : Component
        {
            internal long CurrentLine;

            public int BytesPerLine { get; set; }
            public int LineSpace { get; set; }
            public int MaxCharacters { get; set; }

            public override Size GetSize()
            {
                var width = (int)ImGuiNET.ImGui.CalcTextSize(new string('A', MaxCharacters)).X;

                return new Size(width, 1f);
            }

            protected override void UpdateInternal(Rectangle contentRect)
            {
                var lineHeight = GetTextHeight() + LineSpace;
                for (var row = 0; row < contentRect.Height / lineHeight; row++)
                {
                    ImGuiNET.ImGui.SetCursorPosY(row * lineHeight);
                    ImGuiNET.ImGui.Text(((row + CurrentLine) * BytesPerLine).ToString($"X{MaxCharacters}"));
                }
            }

            private int GetTextHeight()
            {
                return (int)ImGuiNET.ImGui.CalcTextSize("A").Y;
            }
        }

        class PositionListing : Component
        {
            private const int ByteSpacing_ = 3;

            public int BytesPerLine { get; set; }
            public int GroupSize { get; set; }
            public int GroupSpace { get; set; }

            public override Size GetSize()
            {
                return new Size(1f, GetTextHeight());
            }

            protected override void UpdateInternal(Rectangle contentRect)
            {
                var x = ImGuiNET.ImGui.GetCursorPosX();
                var y = ImGuiNET.ImGui.GetCursorPosY();

                var textWidth = GetTextWidth("AA");
                for (var i = 0; i < BytesPerLine; i++)
                {
                    ImGuiNET.ImGui.SetCursorPosX(x);
                    ImGuiNET.ImGui.SetCursorPosY(y);

                    var text = i.ToString("X2");
                    ImGuiNET.ImGui.Text(text);

                    var groupSplit = ByteSpacing_;
                    if (i != 0 && GroupSize > 0 && (i + 1) % GroupSize == 0)
                        groupSplit += GroupSpace;
                    x += textWidth + groupSplit;
                }
            }

            private int GetTextHeight(string input = "A")
            {
                return (int)ImGuiNET.ImGui.CalcTextSize(input).Y;
            }

            private int GetTextWidth(string input)
            {
                return (int)ImGuiNET.ImGui.CalcTextSize(input).X;
            }
        }

        class ByteContainer : Component
        {
            private const int ScrollDelta_ = 4;
            private const int ByteSpacing_ = 3;

            private Stream _data;
            private long _scrollLine;

            public int BytesPerLine { get; set; }
            public int GroupSize { get; set; }
            public int GroupSpace { get; set; }
            public int LineSpace { get; set; }

            public Stream Data
            {
                get => _data;
                set
                {
                    _data = value;
                    _scrollLine = 0;

                    Position = 0;
                }
            }
            public long Position { get; set; }

            public event EventHandler PositionChanged;
            public event EventHandler<MouseScrolledventArgs> MouseScrolled;

            public override Size GetSize()
            {
                return Size.Parent;
            }

            protected override void UpdateInternal(Rectangle contentRect)
            {
                if (Data == null)
                    return;

                var textHeight = GetTextHeight() + LineSpace;
                var textWidth = GetTextWidth("AA");

                // Update scroll delta
                var wheel = ImGuiNET.ImGui.GetIO().MouseWheel;

                // Update scroll line
                if (wheel > 0)
                    _scrollLine -= ScrollDelta_;
                if (wheel < 0)
                    _scrollLine += ScrollDelta_;

                // Get line information
                var lines = contentRect.Height / textHeight;
                var totalLines = Data.Length / BytesPerLine + (Data.Length % BytesPerLine > 0 ? 1 : 0);

                _scrollLine = Math.Max(0, Math.Min(totalLines - lines, _scrollLine));

                // Fire scroll events
                if (wheel != 0)
                    OnMouseScrolled(_scrollLine);

                // Read data
                var bufferLength = Math.Min(Data.Length - _scrollLine * BytesPerLine, lines * BytesPerLine);
                var buffer = new byte[bufferLength];

                var bkPos = Data.Position;
                Data.Position = _scrollLine * BytesPerLine;
                Data.Read(buffer, 0, (int)bufferLength);
                Data.Position = bkPos;

                // Draw bytes from buffer
                var x = ImGuiNET.ImGui.GetCursorPosX();
                var origX = x;
                var y = ImGuiNET.ImGui.GetCursorPosY();

                for (var i = 0; i < buffer.Length; i++)
                {
                    // Draw component
                    ImGuiNET.ImGui.SetCursorPosX(x);
                    ImGuiNET.ImGui.SetCursorPosY(y);

                    var value = $"{buffer[i]:X2}";
                    ImGuiNET.ImGui.Text(value);

                    // Reset position to new value
                    var valueSplit = ByteSpacing_;
                    if (i % BytesPerLine != 0 && GroupSize > 0 && (i % BytesPerLine + 1) % GroupSize == 0)
                        valueSplit += GroupSpace;

                    x += textWidth + valueSplit;
                    if ((i + 1) % BytesPerLine == 0)
                    {
                        y += textHeight;
                        x = origX;
                    }
                }
            }

            private void OnMouseScrolled(long scrollLine)
            {
                MouseScrolled?.Invoke(this, new MouseScrolledventArgs(scrollLine));
            }

            private int GetTextHeight(string input = "A")
            {
                return (int)ImGuiNET.ImGui.CalcTextSize(input).Y;
            }

            private int GetTextWidth(string input)
            {
                return (int)ImGuiNET.ImGui.CalcTextSize(input).X;
            }
        }

        class StringContainer : Component
        {
            internal long CurrentLine;

            public int BytesPerLine { get; set; }

            public int LineSpacing { get; set; }

            public Stream Data { get; set; }

            public long Position { get; set; }

            public override Size GetSize()
            {
                return new Size(GetTextWidth(new string('A', BytesPerLine)), 1f);
            }

            protected override void UpdateInternal(Rectangle contentRect)
            {
                var textWidth = GetTextWidth("A");
                var textHeight = GetTextHeight() + LineSpacing;
                var lines = contentRect.Height / textHeight;

                // Read data
                var bufferLength = Math.Min(Data.Length - CurrentLine * BytesPerLine, lines * BytesPerLine);
                var buffer = new byte[bufferLength];

                var bkPos = Data.Position;
                Data.Position = CurrentLine * BytesPerLine;
                Data.Read(buffer, 0, (int)bufferLength);
                Data.Position = bkPos;

                // Update data
                var x = ImGuiNET.ImGui.GetCursorPosX();
                var origX = x;
                var y = ImGuiNET.ImGui.GetCursorPosY();

                for (var i = 0; i < buffer.Length; i++)
                {
                    // Draw component
                    ImGuiNET.ImGui.SetCursorPosX(x);
                    ImGuiNET.ImGui.SetCursorPosY(y);

                    var value = ((char)buffer[i]).ToString();
                    ImGuiNET.ImGui.Text(value);

                    // Reset position to new value
                    x += textWidth;
                    if ((i + 1) % BytesPerLine == 0)
                    {
                        y += textHeight;
                        x = origX;
                    }
                }
            }

            private int GetTextHeight(string input = "A")
            {
                return (int)ImGuiNET.ImGui.CalcTextSize(input).Y;
            }

            private int GetTextWidth(string input)
            {
                return (int)ImGuiNET.ImGui.CalcTextSize(input).X;
            }
        }

        class MouseScrolledventArgs : EventArgs
        {
            public long ScrollLine { get; }

            public MouseScrolledventArgs(long scrollLine)
            {
                ScrollLine = scrollLine;
            }
        }

        #endregion
    }
}
