using System;
using ImGui.Forms.Controls;
using System.Numerics;
using ImGui.Forms.Models.IO;
using ImGui.Forms.Resources;
using ImGuiNET;
using Veldrid;
using Kuriimu2.ImGui.Resources;

namespace Kuriimu2.ImGui.Components
{
    class ZoomableIndexedPictureBox : ZoomablePictureBox
    {
        public event EventHandler<PixelSelectedEventArgs> PixelSelected;

        protected override void DrawInternal(Rectangle contentRect)
        {
            base.DrawInternal(contentRect);

            if (!HasValidImage())
            {
                DrawControlLegend(contentRect);
                return;
            }

            Rectangle imageRect = GetTransformedImageRect(contentRect);

            Vector2 mousePos = ImGuiNET.ImGui.GetMousePos();
            mousePos = UnTransform(contentRect, mousePos);

            Vector2 imagePos = UnTransform(contentRect, imageRect.Position);

            var x = (int)(mousePos.X - imagePos.X);
            var y = (int)(mousePos.Y - imagePos.Y);

            if (ImGuiNET.ImGui.IsItemHovered())
            {
                if (IsInImage(x, y))
                {
                    bool isSelect = ImGuiNET.ImGui.IsKeyDown(ImGuiKey.ModCtrl);
                    bool isSet = ImGuiNET.ImGui.IsKeyDown(ImGuiKey.ModAlt);

                    if (ImGuiNET.ImGui.IsMouseReleased(ImGuiMouseButton.Left))
                        OnPixelSelected(x, y, isSelect, isSet);
                }
            }

            DrawControlLegend(contentRect);
        }

        protected void OnPixelSelected(int x, int y, bool isSelect, bool isSet)
        {
            PixelSelected?.Invoke(this, new PixelSelectedEventArgs { X = x, Y = y, IsSelect = isSelect, IsSet = isSet });
        }

        private void DrawControlLegend(Rectangle contentRect)
        {
            ImGuiNET.ImGui.GetWindowDrawList().AddText(contentRect.Position, ImGuiNET.ImGui.GetColorU32(ImGuiCol.Text), LocalizationResources.ImagePictureBoxIndexSelectColorControl);
            ImGuiNET.ImGui.GetWindowDrawList().AddText(contentRect.Position + new Vector2(0, TextMeasurer.GetCurrentLineHeight()), ImGuiNET.ImGui.GetColorU32(ImGuiCol.Text), LocalizationResources.ImagePictureBoxIndexSetColorControl);
        }

        private bool IsInImage(float x, float y)
        {
            return x >= 0 && y >= 0 && x < Image!.Width && y < Image.Height;
        }
    }

    public class PixelSelectedEventArgs : EventArgs
    {
        public required int X { get; init; }
        public required int Y { get; init; }
        public required bool IsSelect { get; init; }
        public required bool IsSet { get; init; }
    }
}
