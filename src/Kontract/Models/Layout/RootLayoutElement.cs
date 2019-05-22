using System.Collections.Generic;
using System.Drawing;

namespace Kontract.Models.Layout
{
    public class RootLayoutElement : LayoutElement
    {
        public IList<LayoutElement> Children { get; } = new List<LayoutElement>();

        public RootLayoutElement(Size size, Point location) : base(size, location)
        {
        }

        public override void Draw(Graphics gr,bool drawBorder)
        {
            base.Draw(gr, drawBorder);
            foreach (var child in Children)
            {
                child.Draw(gr, drawBorder);
            }
        }

        public override Point GetAbsoluteLocation()
        {
            return new Point(RelativeLocation.X, RelativeLocation.Y);
        }
    }
}
