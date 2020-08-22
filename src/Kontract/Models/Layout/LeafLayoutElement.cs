using System;
using System.Drawing;

namespace Kontract.Models.Layout
{
    public class LeafLayoutElement : LayoutElement
    {
        public LayoutElement Parent { get; }

        public LocationAnchor ParentAnchor { get; set; } = LocationAnchor.Center;

        public LocationAnchor LocationAnchor { get; set; } = LocationAnchor.Center;

        public LeafLayoutElement(Size size, Point location, LayoutElement parent) : base(size, location)
        {
            Parent = parent;
        }

        public override Point GetAbsoluteLocation()
        {
            var parentPosition = Parent.GetAbsoluteLocation();
            parentPosition += GetAnchorOffset(ParentAnchor, Parent);
            var thisPosition = parentPosition - GetAnchorOffset(LocationAnchor, this);
            return thisPosition + (Size)RelativeLocation;
        }

        private Size GetAnchorOffset(LocationAnchor anchor, LayoutElement layoutItem)
        {
            switch (anchor)
            {
                case LocationAnchor.Top:
                    return new Size(layoutItem.Size.Width / 2, 0);

                case LocationAnchor.Bottom:
                    return new Size(layoutItem.Size.Width / 2, layoutItem.Size.Height);

                case LocationAnchor.Left:
                    return new Size(0, layoutItem.Size.Height / 2);

                case LocationAnchor.Right:
                    return new Size(layoutItem.Size.Width, layoutItem.Size.Height / 2);

                case LocationAnchor.Center:
                    return new Size(layoutItem.Size.Width / 2, layoutItem.Size.Height / 2);

                case LocationAnchor.TopLeft:
                    return new Size(0, 0);

                case LocationAnchor.TopRight:
                    return new Size(layoutItem.Size.Width, 0);

                case LocationAnchor.BottomLeft:
                    return new Size(0, layoutItem.Size.Height);

                case LocationAnchor.BottomRight:
                    return new Size(layoutItem.Size.Width, layoutItem.Size.Height);

                default:
                    throw new NotSupportedException($"{nameof(LocationAnchor)} {anchor} is not supported.");
            }
        }
    }
}
