using System;
using ImGui.Forms.Controls;
using System.Numerics;
using ImGuiNET;
using Veldrid;

namespace Kuriimu2.ImGui.Components
{
    class ZoomableIndexedPictureBox : ZoomablePictureBox
    {
        public event EventHandler<PixelSelectedEventArgs> PixelSelected;

        protected override void UpdateInternal(Rectangle contentRect)
        {
            base.UpdateInternal(contentRect);

            if (!HasValidImage())
                return;

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
                    if (ImGuiNET.ImGui.IsMouseReleased(ImGuiMouseButton.Right))
                    {
                        OnPixelSelected(x, y);
                    }
                }
            }
        }

        protected void OnPixelSelected(int x, int y)
        {
            PixelSelected?.Invoke(this, new PixelSelectedEventArgs { X = x, Y = y });
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
    }
}
