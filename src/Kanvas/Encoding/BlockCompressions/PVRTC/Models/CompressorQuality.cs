﻿namespace Kanvas.Encoding.BlockCompressions.PVRTC.Models
{
    public enum CompressorQuality
    {
        PVRTCFast = 0,
        PVRTCNormal,
        PVRTCHigh,
        PVRTCBest,

        ETCFast = 0,
        ETCFastPerceptual,
        ETCMedium,
        ETCMediumPerceptual,
        ETCSlow,
        ETCSlowPerceptual
    }
}
