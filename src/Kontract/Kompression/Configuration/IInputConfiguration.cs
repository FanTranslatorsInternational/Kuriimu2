namespace Kontract.Kompression.Configuration
{
    public interface IInputConfiguration
    {
        IInputConfiguration Skip(int skip);

        IInputConfiguration Reverse();

        IInputConfiguration Prepend(int byteCount, byte value = 0);

        IInputManipulator Build();
    }
}
