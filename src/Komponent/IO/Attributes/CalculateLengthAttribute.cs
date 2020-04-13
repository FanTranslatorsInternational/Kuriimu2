using System;
using System.Linq;
using Komponent.IO.BinarySupport;

namespace Komponent.IO.Attributes
{
    [AttributeUsage(AttributeTargets.Field)]
    public class CalculateLengthAttribute : Attribute
    {
        public Func<ValueStorage, int> CalculationAction { get; }

        public StringEncoding StringEncoding { get; set; } = StringEncoding.ASCII;

        public CalculateLengthAttribute(Type calculationType, string calculationMethod)
        {
            if (!calculationType.IsClass)
                throw new ArgumentException("Type needs to be a class.");

            if (calculationType.GetConstructors().All(x => x.GetParameters().Length != 0))
                throw new InvalidOperationException("Class needs to have an empty constructor.");

            var method = calculationType.GetMethod(calculationMethod);
            if (method == null)
                throw new InvalidOperationException($"Class does not contain a Method '{calculationMethod}'.");

            var methodParameters = method.GetParameters();
            if (method.ReturnType != typeof(int) ||
                methodParameters.Length != 1 ||
                methodParameters[0].ParameterType != typeof(ValueStorage))
                throw new InvalidOperationException($"Method does not follow the restriction 'int {calculationMethod}(ValueStorage)'.");

            if (calculationType.IsAbstract && calculationType.IsSealed)
            {
                // If class is static
                CalculationAction = storage => (int)method.Invoke(null, new object[] { storage });
            }
            else
            {
                // If class has to be instantiated
                var classInstance = Activator.CreateInstance(calculationType);
                CalculationAction = storage => (int)method.Invoke(classInstance, new object[] { storage });
            }
        }
    }
}
