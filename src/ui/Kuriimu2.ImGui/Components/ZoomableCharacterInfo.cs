using System;
using System.Numerics;
using ImGui.Forms;
using ImGui.Forms.Controls.Base;
using ImGui.Forms.Extensions;
using ImGui.Forms.Resources;
using Konnect.Contract.DataClasses.Plugin.File.Font;
using SixLabors.ImageSharp;
using Rectangle = Veldrid.Rectangle;

namespace Kuriimu2.ImGui.Components
{
    class ZoomableCharacterInfo : ZoomableComponent
    {
        private ImageResource? _glyphResource;

        public CharacterInfo? CharacterInfo { get; private set; }
        public ThemedColor BackgroundColor { get; set; }

        public void SetCharacterInfo(CharacterInfo characterInfo)
        {
            CharacterInfo = characterInfo;

            _glyphResource = characterInfo.Glyph is not null
                ? ImageResource.FromImage(characterInfo.Glyph)
                : null;
        }

        protected override void DrawInternal(Rectangle contentRect)
        {
            if (CharacterInfo == null)
                return;

            int boundingX = Math.Min(CharacterInfo.GlyphPosition.X, 0);
            int boundingY = Math.Min(CharacterInfo.GlyphPosition.Y, 0);
            int boundingWidth = Math.Max(CharacterInfo.GlyphPosition.X, 0) + Math.Max(CharacterInfo.Glyph?.Width ?? 0, CharacterInfo.BoundingBox.Width);
            int boundingHeight = Math.Max(CharacterInfo.GlyphPosition.Y, 0) + Math.Max(CharacterInfo.Glyph?.Height ?? 0, CharacterInfo.BoundingBox.Height);

            var totalBoundingBox = new Rectangle(boundingX, boundingY, boundingWidth - boundingX, boundingHeight - boundingY);

            DrawBackground(contentRect);

            DrawGlyph(contentRect, totalBoundingBox);
            DrawBoundingBox(contentRect, totalBoundingBox);
            DrawTotalBoundingBox(contentRect, totalBoundingBox);
        }

        private void DrawBackground(Rectangle contentRect)
        {
            ImGuiNET.ImGui.GetWindowDrawList().AddRect(contentRect.Position, contentRect.Position + contentRect.Size, BackgroundColor.ToUInt32());
        }

        private void DrawGlyph(Rectangle contentRect, Rectangle totalBoundingBox)
        {
            if (CharacterInfo is null || _glyphResource is null)
                return;

            Vector2 boundingStartPosition = -(new Vector2(totalBoundingBox.Width, totalBoundingBox.Height) / 2) + new Vector2(Math.Max(CharacterInfo.GlyphPosition.X, 0), Math.Max(CharacterInfo.GlyphPosition.Y, 0));
            var imageRect = new Rectangle((int)boundingStartPosition.X, (int)boundingStartPosition.Y, _glyphResource.Width, _glyphResource.Height);
            imageRect = Transform(contentRect, imageRect);

            ImGuiNET.ImGui.GetWindowDrawList().AddImage((nint)_glyphResource, imageRect.Position, imageRect.Position + imageRect.Size);
        }

        private void DrawBoundingBox(Rectangle contentRect, Rectangle totalBoundingBox)
        {
            if (CharacterInfo == null)
                return;

            Vector2 boundingStartPosition = -(new Vector2(totalBoundingBox.Width, totalBoundingBox.Height) / 2) + new Vector2(Math.Max(CharacterInfo.GlyphPosition.X, 0), Math.Max(CharacterInfo.GlyphPosition.Y, 0));
            var imageRect = new Rectangle((int)boundingStartPosition.X, (int)boundingStartPosition.Y, CharacterInfo.BoundingBox.Width, CharacterInfo.BoundingBox.Height);
            imageRect = Transform(contentRect, imageRect);

            ImGuiNET.ImGui.GetWindowDrawList().AddRect(imageRect.Position, imageRect.Position + imageRect.Size, Color.OrangeRed.ToUInt32());
        }

        private void DrawTotalBoundingBox(Rectangle contentRect, Rectangle totalBoundingBox)
        {
            if (CharacterInfo == null)
                return;

            Vector2 boundingStartPosition = -(new Vector2(totalBoundingBox.Width, totalBoundingBox.Height) / 2);
            var imageRect = new Rectangle((int)boundingStartPosition.X, (int)boundingStartPosition.Y, totalBoundingBox.Width, totalBoundingBox.Height);
            imageRect = Transform(contentRect, imageRect);

            ImGuiNET.ImGui.GetWindowDrawList().AddRect(imageRect.Position, imageRect.Position + imageRect.Size, Color.Gold.ToUInt32());
        }
    }
}
