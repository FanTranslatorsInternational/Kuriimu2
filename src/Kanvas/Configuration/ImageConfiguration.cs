using System;
using System.Drawing;
using Kontract;
using Kontract.Kanvas;
using Kontract.Kanvas.Configuration;
using Kontract.Kanvas.Quantization;

namespace Kanvas.Configuration
{
    // TODO: PixelShader
    public class ImageConfiguration : IIndexConfiguration
    {
        private readonly TranscodeConfiguration _transcodeConfiguration;
        private readonly TranscodePaletteConfiguration _transcodePaletteConfiguration;
        private readonly PadSizeConfiguration _padSizeConfiguration;
        private readonly RemapPixelsConfiguration _remapPixelsConfiguration;

        private int _taskCount = Environment.ProcessorCount;
        private IQuantizationConfiguration _quantizationConfiguration;

        private bool IsIndexConfiguration => _transcodeConfiguration.IndexEncoding != null;

        public ITranscodeConfiguration Transcode => _transcodeConfiguration;

        public ITranscodePaletteConfiguration TranscodePalette => _transcodePaletteConfiguration;

        public IPadSizeConfiguration PadSize => _padSizeConfiguration;

        public IRemapPixelsConfiguration RemapPixels => _remapPixelsConfiguration;

        public ImageConfiguration()
        {
            _transcodeConfiguration = new TranscodeConfiguration(this);
            _transcodePaletteConfiguration = new TranscodePaletteConfiguration(this);
            _padSizeConfiguration = new PadSizeConfiguration(this);
            _remapPixelsConfiguration = new RemapPixelsConfiguration(this);
        }

        public IImageConfiguration WithDegreeOfParallelism(int taskCount)
        {
            _taskCount = taskCount;

            return this;
        }

        public IImageConfiguration ConfigureQuantization(Action<IQuantizationOptions> configure)
        {
            ContractAssertions.IsNotNull(configure, nameof(configure));

            _quantizationConfiguration ??= new QuantizationConfiguration();
            _quantizationConfiguration.ConfigureOptions(configure);

            return this;
        }

        public IImageConfiguration WithoutQuantization()
        {
            _quantizationConfiguration = null;

            return this;
        }

        public IImageConfiguration Clone()
        {
            var config = new ImageConfiguration();

            if (_taskCount != Environment.ProcessorCount)
                config.WithDegreeOfParallelism(_taskCount);
            if (_padSizeConfiguration.Delegate != null)
                config.PadSize.With(_padSizeConfiguration.Delegate);
            if (_remapPixelsConfiguration.Delegate != null)
                config.RemapPixels.With(_remapPixelsConfiguration.Delegate);
            if (_quantizationConfiguration != null)
                config.SetQuantizationConfiguration(_quantizationConfiguration);

            if (_transcodeConfiguration.ColorEncoding != null)
                return config.Transcode.With(_transcodeConfiguration.ColorEncoding);

            if (_transcodeConfiguration.IndexEncoding == null)
                return config;

            var indexConfig = config.Transcode.With(_transcodeConfiguration.IndexEncoding);
            return _transcodePaletteConfiguration.PaletteEncoding != null ?
                indexConfig.TranscodePalette.With(_transcodePaletteConfiguration.PaletteEncoding) :
                config;
        }

        public IImageTranscoder Build()
        {
            return IsIndexConfiguration ?
                BuildIndexInternal() :
                BuildColorInternal();
        }

        private IImageTranscoder BuildColorInternal()
        {
            ContractAssertions.IsNotNull(_transcodeConfiguration.ColorEncoding, "colorDelegate");

            // Quantization for normal images is optional
            // If no quantization configuration was done beforehand we assume no quantization to be used here
            var quantizer = _quantizationConfiguration != null ? BuildQuantizer() : null;

            return new ImageTranscoder(_transcodeConfiguration.ColorEncoding, _remapPixelsConfiguration.Delegate, _padSizeConfiguration.Delegate, quantizer, _taskCount);
        }

        private IImageTranscoder BuildIndexInternal()
        {
            ContractAssertions.IsNotNull(_transcodeConfiguration.IndexEncoding, "indexDelegate");
            ContractAssertions.IsNotNull(_transcodePaletteConfiguration.PaletteEncoding, "paletteDelegate");

            var quantizer = BuildQuantizer();

            return new ImageTranscoder(_transcodeConfiguration.IndexEncoding, _transcodePaletteConfiguration.PaletteEncoding, _remapPixelsConfiguration.Delegate, _padSizeConfiguration.Delegate, quantizer, _taskCount);
        }

        private IQuantizer BuildQuantizer()
        {
            var configuration = _quantizationConfiguration ?? new QuantizationConfiguration();

            configuration.WithTaskCount(_taskCount);
            return configuration.Build();
        }

        private void SetQuantizationConfiguration(IQuantizationConfiguration quantizationConfiguration)
        {
            _quantizationConfiguration = quantizationConfiguration.Clone();
        }
    }

    public class TranscodeConfiguration : ITranscodeConfiguration
    {
        private readonly IIndexConfiguration _parent;

        internal IColorEncoding ColorEncoding { get; private set; }

        internal IIndexEncoding IndexEncoding { get; private set; }

        public TranscodeConfiguration(IIndexConfiguration parent)
        {
            _parent = parent;
        }

        public IImageConfiguration With(IColorEncoding encoding)
        {
            ContractAssertions.IsNotNull(encoding, nameof(encoding));

            ColorEncoding = encoding;
            IndexEncoding = null;

            return _parent;
        }

        public IIndexConfiguration With(IIndexEncoding encoding)
        {
            ContractAssertions.IsNotNull(encoding, nameof(encoding));

            IndexEncoding = encoding;
            ColorEncoding = null;

            return _parent;
        }
    }

    public class TranscodePaletteConfiguration : ITranscodePaletteConfiguration
    {
        private readonly IIndexConfiguration _parent;

        internal IColorEncoding PaletteEncoding { get; private set; }

        public TranscodePaletteConfiguration(IIndexConfiguration parent)
        {
            _parent = parent;
        }

        public IIndexConfiguration With(IColorEncoding encoding)
        {
            ContractAssertions.IsNotNull(encoding, nameof(encoding));

            PaletteEncoding = encoding;

            return _parent;
        }
    }

    public class PadSizeConfiguration : IPadSizeConfiguration
    {
        private readonly IImageConfiguration _parent;

        internal CreatePaddedSize Delegate { get; private set; }

        public PadSizeConfiguration(IImageConfiguration parent)
        {
            _parent = parent;
        }

        public IImageConfiguration With(CreatePaddedSize func)
        {
            ContractAssertions.IsNotNull(func, nameof(func));

            Delegate = func;

            return _parent;
        }

        public IImageConfiguration ToPowerOfTwo()
        {
            Delegate = size => new Size(ToPowerOfTwo(size.Width), ToPowerOfTwo(size.Height));

            return _parent;
        }

        public IImageConfiguration ToMultiple(int multiple)
        {
            Delegate = size => new Size(ToMultiple(size.Width, multiple), ToMultiple(size.Height, multiple));

            return _parent;
        }

        private int ToPowerOfTwo(int value)
        {
            return 2 << (int)Math.Log(value - 1, 2);
        }

        private int ToMultiple(int value, int multiple)
        {
            return (value + (multiple - 1)) / multiple * multiple;
        }
    }

    public class RemapPixelsConfiguration : IRemapPixelsConfiguration
    {
        private readonly IImageConfiguration _parent;

        internal CreatePixelRemapper Delegate { get; private set; }

        public RemapPixelsConfiguration(IImageConfiguration parent)
        {
            _parent = parent;
        }

        public IImageConfiguration With(CreatePixelRemapper func)
        {
            ContractAssertions.IsNotNull(func, nameof(func));

            Delegate = func;

            return _parent;
        }
    }
}
