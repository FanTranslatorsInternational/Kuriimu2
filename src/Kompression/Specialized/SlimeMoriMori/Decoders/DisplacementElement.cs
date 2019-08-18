namespace Kompression.Specialized.SlimeMoriMori.Decoders
{
    class DisplacementElement
    {
        public int ReadBits { get; }
        public int DisplacementStart { get; }

        public DisplacementElement(int readBits, int dispalcementStart)
        {
            ReadBits = readBits;
            DisplacementStart = dispalcementStart;
        }
    }
}
