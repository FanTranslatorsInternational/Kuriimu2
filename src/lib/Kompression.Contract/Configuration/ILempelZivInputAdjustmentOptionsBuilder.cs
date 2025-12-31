namespace Kompression.Contract.Configuration
{
    public interface ILempelZivInputAdjustmentOptionsBuilder
    {
        ILempelZivInputAdjustmentOptionsBuilder Skip(int skip);
        ILempelZivInputAdjustmentOptionsBuilder Reverse();
        ILempelZivInputAdjustmentOptionsBuilder Prepend(int byteCount, byte value = 0);
        ILempelZivInputAdjustmentOptionsBuilder Prepend(byte[] data);
    }
}
