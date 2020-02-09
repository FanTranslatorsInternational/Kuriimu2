using Kontract.Kompression;

namespace Kompression.PatternMatch.PriceCalculators
{
    public class TaikoLz81PriceCalculator : IPriceCalculator
    {
        public int CalculateLiteralPrice(int value, int literalRunLength, bool firstLiteralRun)
        {
            // One raw value is encoded with huffman; The huffman code length will be approximated at 6 bits after some heuristics were taken
            // Additionally the length is also huffman coded; We approximate 3 bit for one length index value and to also accomodate
            // for possible coming length indexes
            // Each length can also be followed by additional bits for intermediate lengths, we approximate them with 3 bits
            return 6 + 3 + 3;
        }

        public int CalculateMatchPrice(int displacement, int length, int matchRunLength)
        {
            // One match is encoded with two huffman values
            // The length value can be at max 6 bits, therefore we will approximate it at 3 bits;
            // The displacement value can be at max 5 bits, therefore we will approximate it at 2 bits;
            // For additional bits to reach intermediate values, we approximate 3 bits
            return 3 + 2 + 3;
        }
    }
}
