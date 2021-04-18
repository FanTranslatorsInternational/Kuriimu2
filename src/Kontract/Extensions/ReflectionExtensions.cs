using System;

namespace Kontract.Extensions
{
    public static class ReflectionExtensions
    {
        /// <summary>
        /// Returns if a method with the given methodName defined by the given interfaceType was implemented by this object's type.
        /// </summary>
        public static bool ImplementsMethod(this object o, Type interfaceType, string methodName)
        {
            var type = o.GetType();
            
            // Find the method that {interfaceType}.{methodName} was mapped to
            var target = Array.Find(type.GetInterfaceMap(interfaceType).TargetMethods, method => method.Name == methodName);
            
            if (target == null)
            {
                throw new MissingMethodException($"Method {methodName} does not exist in interface map for {interfaceType.Name} of {type.Name}");
            }
            
            // Check if it was NOT mapped to the interface itself (default implementation)
            return target.DeclaringType != interfaceType;
        }
    }
}