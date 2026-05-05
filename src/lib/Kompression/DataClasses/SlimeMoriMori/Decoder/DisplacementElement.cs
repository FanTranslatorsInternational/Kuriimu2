namespace Kompression.DataClasses.SlimeMoriMori.Decoder
{
    internal class DisplacementElement(int readBits, int displacementStart)
    {
        public int ReadBits { get; } = readBits;
        public int DisplacementStart { get; } = displacementStart;
    }
}
