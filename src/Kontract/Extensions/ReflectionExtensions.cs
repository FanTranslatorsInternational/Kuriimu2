using System.Reflection;

namespace Kontract.Extensions
{
    public static class ReflectionExtensions
    {
        /// <summary>
        /// Returns if the given MethodInfo belongs to an override method.
        /// </summary>
        /// https://stackoverflow.com/a/10020948/10434371
        public static bool IsOverriden(this MethodInfo methodInfo)
        {
            return (methodInfo.GetBaseDefinition() != methodInfo);
        }
    }
}