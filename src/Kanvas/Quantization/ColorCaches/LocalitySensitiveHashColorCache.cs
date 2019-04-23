using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kanvas.Quantization.Helper;
using Kanvas.Quantization.Models.ColorCache;

namespace Kanvas.Quantization.ColorCaches
{
    public class LocalitySensitiveHashColorCache : BaseColorCache
    {
        private const byte DefaultQuality = 16;
        private const long MaximalDistance = 4096;
        private const float NormalizedDistanceRgb = 1.0f / 196608.0f; // 256*256*3 (RGB) = 196608 / 768.0f
        private const float NormalizedDistanceRgba = 1.0f / 262144.0f; // 256*256*4 (RGBA) = 262144 / 1024.0f
        private const float NormalizedDistanceHsl = 1.0f / 260672.0f; // 360*360 (H) + 256*256*2 (SL) = 260672 / 872.0f
        private const float NormalizedDistanceLab = 1.0f / 507.0f; // 13*13*3 = 507 / 300.0f

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

        public LocalitySensitiveHashColorCache()
        {
            Quality = DefaultQuality;
        }

        protected override void OnPrepare()
        {
        }

        protected override void OnCachePalette()
        {
            _buckets = new LshBucketInfo[_quality];

            int paletteIndex = 0;
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

        protected override int CalculatePaletteIndex(Color color)
        {
            var bucket = GetBucket(color);
            var colorCount = bucket.Colors.Count;
            int paletteIndex = 0;

            if (colorCount == 1)
            {
                paletteIndex = bucket.Colors.First().Key;
            }
            else
            {
                int index = 0;
                int colorIndex =
                    ColorModelHelper.GetSmallestEuclideanDistanceIndex(_colorModel, color,
                        bucket.Colors.Values.ToList());

                foreach (var colorPaletteIndex in bucket.Colors.Keys)
                {
                    if (index == colorIndex)
                    {
                        paletteIndex = colorPaletteIndex;
                        break;
                    }

                    index++;
                }
            }

            return paletteIndex;
        }

        private long GetColorBucketIndex(Color color)
        {
            float normalizedDistance;
            switch (_colorModel)
            {
                case ColorModel.RGB: normalizedDistance = NormalizedDistanceRgb; break;
                case ColorModel.RGBA: normalizedDistance = NormalizedDistanceRgba; break;
                //case ColorModel.HueSaturationLuminance: normalizedDistance = NormalizedDistanceHSL; break;
                //case ColorModel.LabColorSpace: normalizedDistance = NormalizedDistanceLab; break;
                default:
                    throw new InvalidOperationException($"ColorModel {_colorModel} not supported.");
            }

            float distance = ColorModelHelper.GetEuclideanDistance(ColorModelHelper.GetColorComponents(_colorModel, color));
            float normalized = distance * normalizedDistance * MaximalDistance;
            long resultHash = (long)normalized / _bucketSize;

            return resultHash;
        }

        private LshBucketInfo GetBucket(Color color)
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
    }
}
