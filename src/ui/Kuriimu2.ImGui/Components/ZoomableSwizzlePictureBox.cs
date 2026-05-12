using System.Collections.Generic;
using System.Numerics;
using Hexa.NET.ImGui;
using ImGui.Forms;
using ImGui.Forms.Controls;
using ImGui.Forms.Extensions;
using Kanvas.Contract;
using SixLabors.ImageSharp;
using Rectangle = ImGui.Forms.Support.Rectangle;

namespace Kuriimu2.ImGui.Components
{
    internal class ZoomableSwizzlePictureBox : ZoomablePictureBox
    {
        private IImageSwizzle? _swizzle;

        public bool RenderSwizzle { get; set; }

        public void SetSwizzle(IImageSwizzle? swizzle) => _swizzle = swizzle;

        protected override void DrawInternal(Rectangle contentRect)
        {
            base.DrawInternal(contentRect);

            if (!HasValidImage())
                return;

            // Render image border
            Rectangle imageRect = GetTransformedImageRect(contentRect);
            Hexa.NET.ImGui.ImGui.GetWindowDrawList().AddRect(imageRect.Position, imageRect.Position + imageRect.Size, Style.GetColor(ImGuiCol.Border).ToUInt32());

            if (!RenderSwizzle || _swizzle is null)
                return;

            // Render swizzle macro block border
            var endPos = new Vector2(_swizzle.MacroTileWidth, _swizzle.MacroTileHeight);
            Rectangle macroBlockRect = Transform(contentRect, new Rectangle(Vector2.Zero, endPos));

            Hexa.NET.ImGui.ImGui.GetWindowDrawList().AddRect(imageRect.Position, imageRect.Position + macroBlockRect.Size, Style.GetColor(ImGuiCol.Border).ToUInt32());

            // Render swizzle path
            DrawSwizzlePath(contentRect, macroBlockRect, imageRect, endPos);
        }

        private void DrawSwizzlePath(Rectangle contentRect, Rectangle macroBlockRect, Rectangle imageRect, Vector2 endPos)
        {
            var points = new List<Vector2>();

            var startCoordinate = new Vector2(.5f, .5f);
            points.Add(Transform(contentRect, startCoordinate) - macroBlockRect.Position + imageRect.Position);

            for (var i = 1; i < Image!.Width * Image!.Height; i++)
            {
                Vector2 swizzledCoordinate = _swizzle!.Get(i);
                Vector2 centeredSwizzledCoordinate = swizzledCoordinate + new Vector2(.5f, .5f);

                points.Add(Transform(contentRect, centeredSwizzledCoordinate) - macroBlockRect.Position + imageRect.Position);

                if (swizzledCoordinate == endPos - Vector2.One)
                    break;
            }

            Vector2[] pointsArray = [.. points];
            Hexa.NET.ImGui.ImGui.GetWindowDrawList().AddPolyline(ref pointsArray[0], points.Count, Color.Red.ToUInt32(), ImDrawFlags.None, 1f);
        }
    }
}
