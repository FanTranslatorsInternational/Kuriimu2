namespace Komponent.Tools
{
    public static class Extensions
    {
        public static void CopyProperties<T>(this T source, T destination)
        {
            foreach (var prop in source.GetType().GetProperties())
                prop.SetValue(destination, prop.GetValue(source));
        }
    }
}
