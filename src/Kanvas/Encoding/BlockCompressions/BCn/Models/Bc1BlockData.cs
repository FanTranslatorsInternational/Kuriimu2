namespace Kanvas.Encoding.BlockCompressions.BCn.Models
{
    public class Bc1BlockData
    {
        public float[] AlphaValues;
        public float[] AlphaErrors;
        public uint AlphaMask;

        public RgbF32[] QuantizedValues = new RgbF32[16];
        public RgbF32[] Error;

        public float[] FDir = new float[4];

        public RgbF32[] InterpValues = new RgbF32[4];

        public RgbF32[] Values = new RgbF32[16];
    }
}
