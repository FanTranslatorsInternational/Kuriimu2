using Hexa.NET.ImGui;
using ImGui.Forms.Controls.Base;
using ImGui.Forms.Extensions;
using ImGui.Forms.Resources;
using Kuriimu2.ImGui.Models.Forms.Dialogs.Font;
using SixLabors.ImageSharp;
using System;
using System.Numerics;
using Rectangle = ImGui.Forms.Support.Rectangle;

namespace Kuriimu2.ImGui.Components
{
    internal class ZoomablePaddedGlyph : ZoomableComponent
    {
        private PaddedGlyph? _paddedGlyph;
        private ImageResource? _paddedGlyphResource;

        public void SetPaddedGlyph(PaddedGlyph? paddedGlyph)
        {
            _paddedGlyph = paddedGlyph;
            _paddedGlyphResource = paddedGlyph?.Glyph == null ? null : ImageResource.FromImage(paddedGlyph.Glyph);
        }

        protected override void DrawInternal(Rectangle contentRect)
        {
            base.DrawInternal(contentRect);

            if (_paddedGlyph == null)
                return;

            int boundingX = Math.Min(_paddedGlyph.GlyphPosition.X, 0);
            int boundingY = Math.Min(_paddedGlyph.GlyphPosition.Y, 0);
            int boundingWidth = Math.Max(_paddedGlyph.GlyphPosition.X, 0) + Math.Max(_paddedGlyph.Glyph?.Width ?? 0, _paddedGlyph.BoundingBox.Width);
            int boundingHeight = Math.Max(_paddedGlyph.GlyphPosition.Y, 0) + Math.Max(_paddedGlyph.Glyph?.Height ?? 0, _paddedGlyph.BoundingBox.Height);

            var totalBoundingBox = new Rectangle(new Vector2(boundingX, boundingY), new Vector2(boundingWidth - boundingX, boundingHeight - boundingY));

            DrawGlyph(contentRect);
            DrawBoundingBox(contentRect);
            DrawTotalBoundingBox(contentRect, totalBoundingBox);

            DrawBaseline(contentRect, totalBoundingBox);

            DrawMousePixelPosition(contentRect, totalBoundingBox);
        }

        private void DrawGlyph(Rectangle contentRect)
        {
            if (_paddedGlyph == null || _paddedGlyphResource == null)
                return;

            Vector2 boundingStartPosition = -(new Vector2(_paddedGlyph.BoundingBox.Width, _paddedGlyph.BoundingBox.Height) / 2) + new Vector2(Math.Max(_paddedGlyph.GlyphPosition.X, 0), Math.Max(_paddedGlyph.GlyphPosition.Y, 0));
            var imageRect = new Rectangle(boundingStartPosition, _paddedGlyphResource.Size);
            imageRect = Transform(contentRect, imageRect);

            Hexa.NET.ImGui.ImGui.GetWindowDrawList().AddImage(_paddedGlyphResource.GetTextureRef(), imageRect.Position, imageRect.Position + imageRect.Size);
        }

        private void DrawBoundingBox(Rectangle contentRect)
        {
            if (_paddedGlyph == null)
                return;

            Vector2 boundingStartPosition = -(new Vector2(_paddedGlyph.BoundingBox.Width, _paddedGlyph.BoundingBox.Height) / 2) + new Vector2(Math.Max(_paddedGlyph.GlyphPosition.X, 0), Math.Max(_paddedGlyph.GlyphPosition.Y, 0));
            var imageRect = new Rectangle(boundingStartPosition, new Vector2(_paddedGlyph.BoundingBox.Width, _paddedGlyph.BoundingBox.Height));
            imageRect = Transform(contentRect, imageRect);

            Hexa.NET.ImGui.ImGui.GetWindowDrawList().AddRect(imageRect.Position, imageRect.Position + imageRect.Size, Color.OrangeRed.ToUInt32() & 0x00FFFFFF | 0x7F000000);
        }

        private void DrawTotalBoundingBox(Rectangle contentRect, Rectangle totalBoundingBox)
        {
            if (_paddedGlyph == null)
                return;

            Vector2 boundingStartPosition = -(new Vector2(totalBoundingBox.Width, totalBoundingBox.Height) / 2);
            var imageRect = new Rectangle(boundingStartPosition, totalBoundingBox.Size);
            imageRect = Transform(contentRect, imageRect);

            Hexa.NET.ImGui.ImGui.GetWindowDrawList().AddRect(imageRect.Position, imageRect.Position + imageRect.Size, Color.Gold.ToUInt32() & 0x00FFFFFF | 0x7F000000);
        }

        private void DrawBaseline(Rectangle contentRect, Rectangle totalBoundingBox)
        {
            Vector2 boundingStartPosition = -(new Vector2(totalBoundingBox.Width, totalBoundingBox.Height) / 2);
            var baseLineRect = new Rectangle(boundingStartPosition with { Y = boundingStartPosition.Y + _paddedGlyph!.Baseline }, new Vector2(totalBoundingBox.Width, 0));
            var baseLineRectTransformed = Transform(contentRect, baseLineRect);

            Hexa.NET.ImGui.ImGui.GetWindowDrawList().AddLine(baseLineRectTransformed.Position, baseLineRectTransformed.Position + baseLineRectTransformed.Size,
                Color.Red.ToUInt32(), 3f);
        }

        private void DrawMousePixelPosition(Rectangle contentRect, Rectangle totalBoundingBox)
        {
            if (!Hexa.NET.ImGui.ImGui.IsItemHovered())
                return;

            var mousePos = Hexa.NET.ImGui.ImGui.GetMousePos();
            if (!contentRect.Contains(mousePos))
                return;

            Vector2 boundingStartPosition = new Vector2(totalBoundingBox.Width, totalBoundingBox.Height) / 2;

            mousePos = UnTransform(contentRect, mousePos);
            mousePos += boundingStartPosition;

            Hexa.NET.ImGui.ImGui.GetWindowDrawList().AddText(contentRect.Position, Hexa.NET.ImGui.ImGui.GetColorU32(ImGuiCol.Text), $"X: {(int)Math.Floor(mousePos.X)}");
            Hexa.NET.ImGui.ImGui.GetWindowDrawList().AddText(contentRect.Position + new Vector2(0, TextMeasurer.GetCurrentLineHeight()), Hexa.NET.ImGui.ImGui.GetColorU32(ImGuiCol.Text), $"Y: {(int)Math.Floor(mousePos.Y)}");
        }
    }
}
