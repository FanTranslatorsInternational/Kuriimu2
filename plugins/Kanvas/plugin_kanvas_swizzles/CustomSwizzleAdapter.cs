using System.Collections.Generic;
using System.Drawing;
#if NET_CORE_21
using System.Composition;
#else
using System.ComponentModel.Composition;
#endif
using Kanvas.Interface;
using Kontract.Attributes.Intermediate;
using Kontract.Interfaces;
using Kontract.Interfaces.Intermediate;

namespace plugin_kanvas_swizzles
{
    [Export(typeof(IPlugin))]
    [Property(nameof(Width), typeof(int), 1)]
    [Property(nameof(Height), typeof(int), 1)]
    public class CustomSwizzleAdapter : IImageSwizzleAdapter
    {
        public IImageSwizzle Swizzle { get; private set; }

        private int _createSwizzle;
        private int _width;
        private int _height;
        private List<(int, int)> _bitField=new List<(int, int)>();

        public string Name => "Custom";

        public int Width
        {
            get => _width;
            set
            {
                _width = value;
                if (_createSwizzle > 1) CreateSwizzle();
                else _createSwizzle++;
            }
        }

        public int Height
        {
            get => _height;
            set
            {
                _height = value;
                if (_createSwizzle > 1) CreateSwizzle();
                else _createSwizzle++;
            }
        }

        public List<(int, int)> BitField
        {
            get => _bitField;
            set
            {
                _bitField = value;
                CreateSwizzle();
            }
        }

        public Point TransformPoint(Point point)
        {
            return Swizzle?.Get(point) ?? Point.Empty;
        }

        private void CreateSwizzle()
        {
            Swizzle = new CustomSwizzle(_width, _height, _bitField.ToArray());
        }
    }
}
