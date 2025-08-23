using System;
using System.Collections.Generic;
using System.Numerics;
using ImGui.Forms.Controls;
using ImGui.Forms.Controls.Base;
using ImGui.Forms.Extensions;
using ImGuiNET;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Rectangle = Veldrid.Rectangle;
using Size = ImGui.Forms.Models.Size;

namespace Kuriimu2.ImGui.Components
{
    class PaletteView : Component
    {
        private readonly ColorPicker _colorPicker;

        private Vector2? _previousSize;
        private Vector2? _previousSpacing;
        private int? _previousColorCount;

        private Vector2 _colorSize;
        private int _cols;
        private int _rows;

        private int _selectedColorIndex = -1;

        public IList<Rgba32>? Palette { get; set; }
        public int SelectedIndex => _selectedColorIndex;

        public Vector2 Spacing { get; set; }

        public event EventHandler<int> ColorChanged;

        public PaletteView()
        {
            _colorPicker = new();
            _colorPicker.ColorChanged += _colorPicker_ColorChanged;
        }

        public override Size GetSize() => Size.Parent;

        public void SetSelectedColor(Rgba32 color)
        {
            if (Palette is null)
                return;

            int colorIndex = Palette.IndexOf(color);
            if (colorIndex < 0)
                return;

            _selectedColorIndex = colorIndex;
        }

        protected override void UpdateInternal(Rectangle contentRect)
        {
            ImGuiNET.ImGui.Dummy(contentRect.Size);

            if (Palette is null || Palette.Count <= 0)
                return;

            if (!_previousSize.HasValue || _previousSize.Value != contentRect.Size
                || (_previousSpacing.HasValue && _previousSpacing.Value != Spacing)
                || (_previousColorCount.HasValue && _previousColorCount.Value != Palette.Count))
                _colorSize = CalculateColorSize(contentRect.Size, Palette!.Count, out _cols, out _rows);

            Vector2 colorPos = contentRect.Position;
            for (var row = 0; row < _rows; row++)
            {
                var shouldBreak = false;
                for (var col = 0; col < _cols; col++)
                {
                    int index = row * _cols + col;
                    if (index >= Palette.Count)
                    {
                        shouldBreak = true;
                        break;
                    }

                    if (ImGuiNET.ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByPopup)
                        && ImGuiNET.ImGui.IsMouseHoveringRect(colorPos, colorPos + _colorSize))
                    {
                        if (ImGuiNET.ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                        {
                            if (_selectedColorIndex == index)
                                _selectedColorIndex = -1;
                            else
                                _selectedColorIndex = index;
                        }
                        else if (ImGuiNET.ImGui.IsMouseClicked(ImGuiMouseButton.Right))
                        {
                            if (_selectedColorIndex != index)
                                _selectedColorIndex = index;
                        }
                    }

                    ImGuiNET.ImGui.GetWindowDrawList().AddRectFilled(colorPos, colorPos + _colorSize, ((Color)Palette[index]).ToUInt32());

                    colorPos = colorPos with { X = colorPos.X + _colorSize.X + Spacing.X };
                }

                if (shouldBreak)
                    break;

                colorPos = new Vector2(contentRect.X, colorPos.Y + _colorSize.Y + Spacing.Y);
            }

            if (_selectedColorIndex >= 0)
            {
                var spacedColorSize = new Vector2(_colorSize.X + Spacing.X, _colorSize.Y + Spacing.Y);
                Vector2 selectedColorPos = new Vector2(_selectedColorIndex % _cols, _selectedColorIndex / _cols) * spacedColorSize + contentRect.Position;

                ImGuiNET.ImGui.GetWindowDrawList().AddRect(selectedColorPos, selectedColorPos + _colorSize, Color.Red.ToUInt32(), 0f, ImDrawFlags.None, 2f);
            }

            if (_selectedColorIndex >= 0 && ImGuiNET.ImGui.BeginPopupContextWindow($"{Id}context", ImGuiPopupFlags.NoOpenOverExistingPopup | ImGuiPopupFlags.MouseButtonRight))
            {
                int width = _colorPicker.GetWidth(contentRect.Width, contentRect.Height);
                int height = _colorPicker.GetHeight(contentRect.Width, contentRect.Height);
                var pos = ImGuiNET.ImGui.GetCursorPos();

                _colorPicker.PickedColor = Palette[_selectedColorIndex];
                _colorPicker.Update(new Rectangle((int)pos.X, (int)pos.Y, width, height));

                ImGuiNET.ImGui.EndPopup();
            }

            _previousSize = contentRect.Size;
            _previousSpacing = Spacing;
            _previousColorCount = Palette.Count;
        }

        private Vector2 CalculateColorSize(Vector2 size, int colorCount, out int bestCols, out int bestRows)
        {
            var bestS = 0f;
            bestCols = 1;
            bestRows = colorCount;

            for (var cols = 1; cols <= colorCount; cols++)
            {
                var rows = (int)Math.Ceiling(colorCount / (float)cols);

                // If the spacing alone already exceeds the width/height, skip
                float remW = size.X - (cols - 1) * Spacing.X;
                float remH = size.Y - (rows - 1) * Spacing.Y;
                if (remW <= 0 || remH <= 0)
                    continue;

                float s = Math.Min(remW / cols, remH / rows);
                if (s > bestS)
                {
                    bestS = s;
                    bestCols = cols;
                    bestRows = rows;
                }
            }

            return new Vector2(bestS);
        }

        private void _colorPicker_ColorChanged(object? sender, EventArgs e)
        {
            if (_selectedColorIndex < 0 || Palette is null || _selectedColorIndex >= Palette.Count)
                return;

            if (Palette[_selectedColorIndex] == _colorPicker.PickedColor)
                return;

            Palette[_selectedColorIndex] = _colorPicker.PickedColor;

            OnColorChanged(_selectedColorIndex);
        }

        private void OnColorChanged(int colorIndex)
        {
            ColorChanged?.Invoke(this, colorIndex);
        }
    }
}
