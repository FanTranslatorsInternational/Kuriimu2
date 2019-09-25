#if NET_CORE_21
using System.Composition;
#else
using System.ComponentModel.Composition;
#endif
using System.Drawing;
using Kanvas.Interface;
using Kanvas.Swizzle;
using Kanvas.Swizzle.Models;
using Kontract.Attributes.Intermediate;
using Kontract.Interfaces;
using Kontract.Interfaces.Intermediate;

namespace plugin_kanvas_swizzles
{
    [Export(typeof(IPlugin))]
    [Property(nameof(Width), typeof(int), 1)]
    [Property(nameof(Height), typeof(int), 1)]
    [Property(nameof(BitDepth), typeof(int), 8)]
    [Property(nameof(SwitchFormat), typeof(SwitchFormat), SwitchFormat.RGBA8888)]
    [Property(nameof(ToPowerOf2), typeof(bool), true)]
    public class SwitchSwizzleAdapter : IImageSwizzleAdapter
    {
        private int _width;
        private int _height;
        private int _bitDepth;
        private SwitchFormat _format;
        private bool _toPower;

        private byte _createSwizzle;

        public IImageSwizzle Swizzle { get; private set; }

        public string Name => "Switch";

        public int Width
        {
            get => _width;
            set
            {
                _width = value;
                if (_createSwizzle > 3) CreateSwizzle();
                else _createSwizzle++;
            }
        }

        public int Height
        {
            get => _height;
            set
            {
                _height = value;
                if (_createSwizzle > 3) CreateSwizzle();
                else _createSwizzle++;
            }
        }
        public int BitDepth
        {
            get => _bitDepth;
            set
            {
                _bitDepth = value;
                if (_createSwizzle > 3) CreateSwizzle();
                else _createSwizzle++;
            }
        }
        public SwitchFormat SwitchFormat
        {
            get => _format;
            set
            {
                _format = value;
                if (_createSwizzle > 3) CreateSwizzle();
                else _createSwizzle++;
            }
        }
        public bool ToPowerOf2
        {
            get => _toPower;
            set
            {
                _toPower = value;
                if (_createSwizzle > 3) CreateSwizzle();
                else _createSwizzle++;
            }
        }

        public Point TransformPoint(Point point)
        {
            return Swizzle?.Get(point) ?? Point.Empty;
        }

        private void CreateSwizzle()
        {
            Swizzle = new SwitchSwizzle(_width, _height, _bitDepth, _format, _toPower);
        }
    }
}
