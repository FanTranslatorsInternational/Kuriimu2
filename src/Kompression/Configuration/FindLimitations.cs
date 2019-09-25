namespace Kompression.Configuration
{
    public class FindLimitations
    {
        public int MinLength { get; }
        public int MaxLength { get; }
        public int MinDisplacement { get; }
        public int MaxDisplacement { get; }

        public FindLimitations(int minLength, int maxLength)
        {
            MinLength = minLength;
            MaxLength = maxLength;
        }

        public FindLimitations(int minLength, int maxLength, int minDisplacement, int maxDisplacement) : this(minLength, maxLength)
        {
            MinDisplacement = minDisplacement;
            MaxDisplacement = maxDisplacement;
        }
    }
}
