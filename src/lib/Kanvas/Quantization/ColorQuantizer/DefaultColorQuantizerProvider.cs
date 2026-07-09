using Kanvas.Contract.Configuration;
using Kanvas.DataClasses.Configuration;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Kanvas.Quantization.ColorQuantizer;

internal static class DefaultColorQuantizerProvider
{
    private const int AlphaDiversityThreshold = 16;
    private const int LowVarianceMaxDistinctAlpha = 2;
    private const double MostlyOpaqueRatioThreshold = 0.98;
    private const int HighAlphaBitDepthThreshold = 8;
    private const int LowAlphaBitDepthThreshold = 2;
    private const int SampleLimit = 200000;
    private const double GradientHeavyUniqueRgbRatioThreshold = 0.45;
    private const double FlatArtUniqueRgbRatioThreshold = 0.18;
    private const double GradientHeavyAvgDeltaThreshold = 24.0;
    private const double FlatArtAvgDeltaThreshold = 10.0;

    public static CreateColorQuantizerDelegate Get(IList<Rgba32> colors, Size imageSize, QuantizationConfigurationOptions options)
    {
        // 1) If source alpha diversity is high (many distinct alpha levels), prefer Distinct Selection.
        // 2) If source contains mostly opaque pixels with low alpha variance, prefer Wu.
        // 3) If target palette encoding has very limited alpha depth (e.g. 1-2 bits), prefer Wu.
        // 4) If target palette encoding stores full alpha (e.g. 8-bit), prefer Distinct Selection.
        // 5) If source appears gradient-heavy in RGB channels, prefer Wu for smoother gradients.
        // 6) If source is mostly flat-color / UI-like art, prefer Distinct Selection.
        // 8) If deterministic color retention for sparse colors is prioritized, prefer Distinct Selection.
        CreateColorQuantizerDelegate wuQuantizer = (colorCount, _, colorChannelBitDepths) =>
            new WuColorQuantizer(colorChannelBitDepths, colorCount);
        CreateColorQuantizerDelegate distinctQuantizer = (colorCount, taskCount, _) =>
            new DistinctSelectionColorQuantizer(colorCount, taskCount);

        if (colors.Count <= 0)
            return wuQuantizer;

        int sampleStep = Math.Max(1, colors.Count / SampleLimit);
        var distinctAlphaValues = new HashSet<byte>();
        var distinctRgbValues = new HashSet<int>();

        long rgbDeltaSum = 0;
        int transitionCount = 0;
        int sampledColorCount = 0;
        int fullyOpaqueColorCount = 0;
        Rgba32? previousColor = null;

        for (int i = 0; i < colors.Count; i += sampleStep)
        {
            Rgba32 color = colors[i];

            sampledColorCount++;
            if (color.A == byte.MaxValue)
                fullyOpaqueColorCount++;

            distinctAlphaValues.Add(color.A);
            distinctRgbValues.Add((color.R << 16) | (color.G << 8) | color.B);

            if (previousColor != null)
            {
                Rgba32 previous = previousColor.Value;
                rgbDeltaSum += Math.Abs(color.R - previous.R) + Math.Abs(color.G - previous.G) + Math.Abs(color.B - previous.B);
                transitionCount++;
            }

            previousColor = color;
        }

        if (sampledColorCount <= 0)
            return wuQuantizer;

        int alphaBitDepth = options.ColorChannelBitDepths.Alpha;
        double opaqueRatio = (double)fullyOpaqueColorCount / sampledColorCount;
        double avgRgbDelta = transitionCount == 0 ? 0 : (double)rgbDeltaSum / transitionCount;
        double uniqueRgbRatio = (double)distinctRgbValues.Count / sampledColorCount;

        if (alphaBitDepth is > 0 and <= LowAlphaBitDepthThreshold)
            return wuQuantizer;

        if (options.InitialPaletteDelegate != null)
            return distinctQuantizer;

        if (alphaBitDepth >= HighAlphaBitDepthThreshold && opaqueRatio < 1.0)
            return distinctQuantizer;

        if (distinctAlphaValues.Count >= AlphaDiversityThreshold)
            return distinctQuantizer;

        if (opaqueRatio >= MostlyOpaqueRatioThreshold && distinctAlphaValues.Count <= LowVarianceMaxDistinctAlpha)
            return wuQuantizer;

        if (avgRgbDelta >= GradientHeavyAvgDeltaThreshold && uniqueRgbRatio >= GradientHeavyUniqueRgbRatioThreshold)
            return wuQuantizer;

        if (avgRgbDelta <= FlatArtAvgDeltaThreshold && uniqueRgbRatio <= FlatArtUniqueRgbRatioThreshold)
            return distinctQuantizer;

        return wuQuantizer;
    }
}
