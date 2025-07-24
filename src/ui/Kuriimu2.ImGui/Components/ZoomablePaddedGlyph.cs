using System;
using System.Numerics;
using ImGui.Forms.Controls.Base;
using ImGui.Forms.Extensions;
using ImGui.Forms.Resources;
using Konnect.Contract.DataClasses.Plugin.File.Font;
using Kuriimu2.ImGui.Models.Forms.Dialogs.Font;
using SixLabors.ImageSharp;
using Rectangle = Veldrid.Rectangle;

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
            if (_paddedGlyph == null)
                return;

            int boundingX = Math.Min(_paddedGlyph.GlyphPosition.X, 0);
            int boundingY = Math.Min(_paddedGlyph.GlyphPosition.Y, 0);
            int boundingWidth = Math.Max(_paddedGlyph.GlyphPosition.X, 0) + Math.Max(_paddedGlyph.Glyph?.Width ?? 0, _paddedGlyph.BoundingBox.Width);
            int boundingHeight = Math.Max(_paddedGlyph.GlyphPosition.Y, 0) + Math.Max(_paddedGlyph.Glyph?.Height ?? 0, _paddedGlyph.BoundingBox.Height);

            var totalBoundingBox = new Rectangle(boundingX, boundingY, boundingWidth - boundingX, boundingHeight - boundingY);

            DrawGlyph(contentRect);
            DrawBoundingBox(contentRect);
            DrawTotalBoundingBox(contentRect, totalBoundingBox);

            DrawBaseline(contentRect, totalBoundingBox);
        }

        private void DrawGlyph(Rectangle contentRect)
        {
            if (_paddedGlyph == null || _paddedGlyphResource == null)
                return;

            Vector2 boundingStartPosition = -(new Vector2(_paddedGlyph.BoundingBox.Width, _paddedGlyph.BoundingBox.Height) / 2) + new Vector2(Math.Max(_paddedGlyph.GlyphPosition.X, 0), Math.Max(_paddedGlyph.GlyphPosition.Y, 0));
            var imageRect = new Rectangle((int)boundingStartPosition.X, (int)boundingStartPosition.Y, _paddedGlyphResource.Width, _paddedGlyphResource.Height);
            imageRect = Transform(contentRect, imageRect);

            ImGuiNET.ImGui.GetWindowDrawList().AddImage((nint)_paddedGlyphResource, imageRect.Position, imageRect.Position + imageRect.Size);
        }

        private void DrawBoundingBox(Rectangle contentRect)
        {
            if (_paddedGlyph == null)
                return;

            Vector2 boundingStartPosition = -(new Vector2(_paddedGlyph.BoundingBox.Width, _paddedGlyph.BoundingBox.Height) / 2) + new Vector2(Math.Max(_paddedGlyph.GlyphPosition.X, 0), Math.Max(_paddedGlyph.GlyphPosition.Y, 0));
            var imageRect = new Rectangle((int)boundingStartPosition.X, (int)boundingStartPosition.Y, _paddedGlyph.BoundingBox.Width, _paddedGlyph.BoundingBox.Height);
            imageRect = Transform(contentRect, imageRect);

            ImGuiNET.ImGui.GetWindowDrawList().AddRect(imageRect.Position, imageRect.Position + imageRect.Size, Color.OrangeRed.ToUInt32(), 5f);
        }

        private void DrawTotalBoundingBox(Rectangle contentRect, Rectangle totalBoundingBox)
        {
            if (_paddedGlyph == null)
                return;

            Vector2 boundingStartPosition = -(new Vector2(totalBoundingBox.Width, totalBoundingBox.Height) / 2);
            var imageRect = new Rectangle((int)boundingStartPosition.X, (int)boundingStartPosition.Y, totalBoundingBox.Width, totalBoundingBox.Height);
            imageRect = Transform(contentRect, imageRect);

            ImGuiNET.ImGui.GetWindowDrawList().AddRect(imageRect.Position, imageRect.Position + imageRect.Size, Color.Gold.ToUInt32(), 5f);
        }

        private void DrawBaseline(Rectangle contentRect, Rectangle totalBoundingBox)
        {
            Vector2 boundingStartPosition = -(new Vector2(totalBoundingBox.Width, totalBoundingBox.Height) / 2);
            var baseLineRect = new Rectangle((int)boundingStartPosition.X, (int)boundingStartPosition.Y + _paddedGlyph!.Baseline, totalBoundingBox.Width, 0);
            var baseLineRectTransformed = Transform(contentRect, baseLineRect);

            ImGuiNET.ImGui.GetWindowDrawList().AddLine(baseLineRectTransformed.Position, baseLineRectTransformed.Position + baseLineRectTransformed.Size,
                Color.Red.ToUInt32(), 3f);
        }
    }
}
