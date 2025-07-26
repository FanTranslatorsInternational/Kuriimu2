using Kanvas.Enums.Quantization.ColorCache.LocalSensitivityHash;
using Kanvas.Quantization.ColorCache.EuclideanDistance;
using Kanvas.Quantization.ColorCache.LocalSensitivityHash;
using SixLabors.ImageSharp.PixelFormats;

namespace Kanvas.Quantization.ColorCache
{
    public class LocalitySensitiveHashColorCache : ColorCache
    {
        private const byte DefaultQuality = 16;
        private const long MaximalDistance = 4096;
        private const float NormalizedDistanceRgb = 1.0f / 196608.0f; // 256*256*3 (RGB) = 196608 / 768.0f
        private const float NormalizedDistanceRgba = 1.0f / 262144.0f; // 256*256*4 (Rgba) = 262144 / 1024.0f
        private const float NormalizedDistanceHsl = 1.0f / 260672.0f; // 360*360 (H) + 256*256*2 (SL) = 260672 / 872.0f
        private const float NormalizedDistanceLab = 1.0f / 507.0f; // 13*13*3 = 507 / 300.0f

        private readonly ColorModel _colorModel;

        private byte _quality;
        private long _bucketSize;
        private long _minBucketIndex;
        private long _maxBucketIndex;
        private LshBucketInfo[] _buckets;

        /// <summary>
        /// Gets or sets the quality.
        /// </summary>
        /// <value>The quality.</value>
        public byte Quality
        {
            get => _quality;
            set
            {
                _quality = value;

                _bucketSize = MaximalDistance / _quality;
                _minBucketIndex = _quality;
                _maxBucketIndex = 0;

                _buckets = new LshBucketInfo[_quality];
            }
        }

        public LocalitySensitiveHashColorCache(IList<Rgba32> palette, ColorModel colorModel) :
            base(palette)
        {
            Quality = DefaultQuality;
            _colorModel = colorModel;

            CreateBuckets(palette);
        }

        /// <inheritdoc />
        public override int GetPaletteIndex(Rgba32 color)
        {
            var bucket = GetBucket(color);

            if (bucket.Colors.Count == 1)
                return bucket.Colors.First().Key;

            var bucketColors = bucket.Colors.Values.ToArray();
            var bucketIndex = EuclideanHelper.GetSmallestEuclideanDistanceIndex(bucketColors, color);

            return bucket.Colors.ElementAt(bucketIndex).Key;
        }

        private void CreateBuckets(IList<Rgba32> palette)
        {
            _buckets = new LshBucketInfo[_quality];

            var paletteIndex = 0;
            _minBucketIndex = _quality;
            _maxBucketIndex = 0;

            foreach (var color in Palette)
            {
                long bucketIndex = GetColorBucketIndex(color);
                var bucket = _buckets[bucketIndex] ?? new LshBucketInfo();
                bucket.AddColor(paletteIndex++, color);
                _buckets[bucketIndex] = bucket;

                if (bucketIndex < _minBucketIndex) _minBucketIndex = bucketIndex;
                if (bucketIndex > _maxBucketIndex) _maxBucketIndex = bucketIndex;
            }
        }


        private LshBucketInfo GetBucket(Rgba32 color)
        {
            long bucketIndex = GetColorBucketIndex(color);

            if (bucketIndex < _minBucketIndex)
            {
                bucketIndex = _minBucketIndex;
            }
            else if (bucketIndex > _maxBucketIndex)
            {
                bucketIndex = _maxBucketIndex;
            }
            else if (_buckets[bucketIndex] == null)
            {
                var bottomFound = false;
                var topFound = false;
                long bottomBucketIndex = bucketIndex;
                long topBucketIndex = bucketIndex;

                while (!bottomFound && !topFound)
                {
                    bottomBucketIndex--;
                    topBucketIndex++;
                    bottomFound = bottomBucketIndex > 0 && _buckets[bottomBucketIndex] != null;
                    topFound = topBucketIndex < _quality && _buckets[topBucketIndex] != null;
                }

                bucketIndex = bottomFound ? bottomBucketIndex : topBucketIndex;
            }

            return _buckets[bucketIndex];
        }


        private long GetColorBucketIndex(Rgba32 color)
        {
            var normalizedDistance = GetNormalizedDistance();
            var distance = EuclideanHelper.GetEuclideanDistance(color);

            var normalized = distance * normalizedDistance * MaximalDistance;
            var bucketIndex = (long)normalized / _bucketSize;

            return bucketIndex;
        }

        private float GetNormalizedDistance()
        {
            switch (_colorModel)
            {
                case ColorModel.Rgb: return NormalizedDistanceRgb;
                case ColorModel.Rgba: return NormalizedDistanceRgba;
                //case ColorModel.HSL: return NormalizedDistanceHsl;
                //case ColorModel.Lab: return NormalizedDistanceLab;
                default: return 0;
            }
        }
    }
}
