using System.Numerics;
using ImGui.Forms;
using ImGui.Forms.Controls.Base;
using ImGui.Forms.Extensions;
using ImGui.Forms.Models;
using ImGui.Forms.Resources;
using ImGuiNET;
using Konnect.Contract.Plugin.File.Image;
using Kuriimu2.ImGui.Resources;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Rectangle = Veldrid.Rectangle;
using Size = ImGui.Forms.Models.Size;

namespace Kuriimu2.ImGui.Components
{
    internal class ImageThumbnail : Component
    {
        private readonly int _index;

        private ImageResource _thumbnail;

        public IImageFile ImageFile { get; }

        public Vector2 ThumbnailSize { get; } = new(90, 60);
        public bool ShowThumbnailBorder { get; set; } = true;

        public string Name => ImageFile.ImageInfo.Name ?? $"{_index:00}";

        public ImageThumbnail(IImageFile imageFile, int index, Image<Rgba32> image)
        {
            _index = index;

            ImageFile = imageFile;

            _thumbnail = CreateThumbnailResource(image);
        }

        public override Size GetSize() => new(SizeValue.Parent, SizeValue.Absolute((int)ThumbnailSize.Y));

        public void SetThumbnail(Image<Rgba32> image)
        {
            _thumbnail = CreateThumbnailResource(image);
        }

        protected override void UpdateInternal(Rectangle contentRect)
        {
            // Add thumbnail
            Vector2 centerPos = contentRect.Position + new Vector2((ThumbnailSize.X - _thumbnail.Width) / 2, (ThumbnailSize.Y - _thumbnail.Height) / 2);
            ImGuiNET.ImGui.GetWindowDrawList().AddImage((nint)_thumbnail, centerPos, centerPos + _thumbnail.Size);

            if (ShowThumbnailBorder)
                ImGuiNET.ImGui.GetWindowDrawList().AddRect(contentRect.Position, contentRect.Position + ThumbnailSize, Style.GetColor(ImGuiCol.Border).ToUInt32());

            // Add name
            if (Name != null)
            {
                var textHeight = Application.Instance.MainForm.DefaultFont.GetLineHeight();
                var textPosition = contentRect.Position + new Vector2(4, 0) + ThumbnailSize with { Y = ThumbnailSize.Y / 2 - textHeight / 2 };
                var textColor = ImageFile.ImageInfo.ContentChanged
                    ? ColorResources.Changed.ToUInt32()
                    : Style.GetColor(ImGuiCol.Text).ToUInt32();

                ImGuiNET.ImGui.GetWindowDrawList().AddText(textPosition, textColor, Name);
            }
        }

        private ImageResource CreateThumbnailResource(Image<Rgba32> image)
        {
            float scaling = GetAspectScaling(image);

            Image<Rgba32> thumbnail = image.Clone();
            thumbnail.Mutate(context => context.Resize(new SixLabors.ImageSharp.Size((int)(image.Width * scaling), (int)(image.Height * scaling))));

            return ImageResource.FromImage(thumbnail);
        }

        private float GetAspectScaling(Image<Rgba32> image)
        {
            // Calculate aspect ratios
            double imageAspectRatio = image.Width / (float)image.Height;
            double thumbnailAspectRatio = ThumbnailSize.X / ThumbnailSize.Y;

            float scalingFactor;

            // Determine the scaling factor
            if (imageAspectRatio > thumbnailAspectRatio)
            {
                // Scale based on width
                scalingFactor = ThumbnailSize.X / image.Width;
            }
            else
            {
                // Scale based on height
                scalingFactor = ThumbnailSize.Y / image.Height;
            }

            return scalingFactor;
        }
    }
}
