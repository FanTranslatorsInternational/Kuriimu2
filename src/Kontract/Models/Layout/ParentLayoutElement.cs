using System.Collections.Generic;
using System.Drawing;

namespace Kontract.Models.Layout
{
    public class ParentLayoutElement : LeafLayoutElement
    {
        public IList<LayoutElement> Children { get; } = new List<LayoutElement>();

        public ParentLayoutElement(Size size, Point location, LayoutElement parent) : base(size, location, parent)
        {
        }

        public override void Draw(Graphics gr)
        {
            base.Draw(gr);
            foreach (var child in Children)
            {
                child.Draw(gr);
            }
        }
    }
}
