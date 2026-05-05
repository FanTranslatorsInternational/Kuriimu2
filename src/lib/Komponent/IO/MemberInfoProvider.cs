using System.Reflection;
using System.Text;
using Komponent.Contract.Aspects;
using Komponent.Contract.Enums;
using Komponent.DataClasses;

namespace Komponent.IO
{
    internal class MemberInfoProvider
    {
        private readonly Dictionary<(Type, string), Func<ValueStorage, int>> _calculateMethodCache = [];

        static MemberInfoProvider()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        public static ByteOrder GetByteOrder(MemberInfo? member, ByteOrder defaultByteOrder)
        {
            return member?.GetCustomAttribute<EndiannessAttribute>()?.ByteOrder ?? defaultByteOrder;
        }

        public static IList<TypeChoice> GetTypeChoices(MemberInfo? member)
        {
            if (member == null)
                return Array.Empty<TypeChoice>();

            var typeChoices = new List<TypeChoice>();
            foreach (var typeChoice in member.GetCustomAttributes<TypeChoiceAttribute>())
                typeChoices.Add(new TypeChoice(typeChoice.FieldName, typeChoice.Comparer, typeChoice.Value, typeChoice.InjectionType));

            return typeChoices;
        }

        public static LengthInfoSource? GetLengthInfoSource(MemberInfo? member)
        {
            var fixedLengthAttribute = member?.GetCustomAttribute<FixedLengthAttribute>();
            var variableLengthAttribute = member?.GetCustomAttribute<VariableLengthAttribute>();
            var calculatedLengthAttribute = member?.GetCustomAttribute<CalculateLengthAttribute>();

            if (fixedLengthAttribute != null)
                return LengthInfoSource.Fixed;

            if (variableLengthAttribute != null)
                return LengthInfoSource.Variable;

            if (calculatedLengthAttribute != null)
                return LengthInfoSource.Calculation;

            return null;
        }

        public LengthInfo? GetLengthInfo(MemberInfo? member, ValueStorage? values)
        {
            var fixedLengthAttribute = member?.GetCustomAttribute<FixedLengthAttribute>();
            var variableLengthAttribute = member?.GetCustomAttribute<VariableLengthAttribute>();
            var calculatedLengthAttribute = member?.GetCustomAttribute<CalculateLengthAttribute>();

            Encoding encoding;
            int length;

            if (fixedLengthAttribute != null)
            {
                encoding = GetEncoding(fixedLengthAttribute.StringEncoding);
                length = fixedLengthAttribute.Length;
            }
            else if (variableLengthAttribute != null)
            {
                if (values == null)
                    throw new InvalidOperationException("Value storage is necessary to determine variable length.");

                encoding = GetEncoding(variableLengthAttribute.StringEncoding);
                length = Convert.ToInt32(values.Get(variableLengthAttribute.FieldName)) + variableLengthAttribute.Offset;
            }
            else if (calculatedLengthAttribute != null)
            {
                if (values == null)
                    throw new InvalidOperationException("Value storage is necessary to calculate length.");

                encoding = GetEncoding(calculatedLengthAttribute.StringEncoding);
                length = ResolveCalculateLengthAttributeMethod(calculatedLengthAttribute)(values);
            }
            else
            {
                return null;
            }

            return new LengthInfo(length, encoding);
        }

        public static BitFieldInfo? GetBitFieldInfo(MemberInfo? member)
        {
            var bitFieldInfoAttribute = member?.GetCustomAttribute<BitFieldInfoAttribute>();
            if (bitFieldInfoAttribute == null)
                return null;

            return new BitFieldInfo
            {
                BitOrder = bitFieldInfoAttribute.BitOrder,
                BlockSize = bitFieldInfoAttribute.BlockSize
            };
        }

        public static int? GetBitLength(MemberInfo? member)
        {
            return member?.GetCustomAttribute<BitFieldAttribute>()?.BitLength;
        }

        public static int? GetAlignment(MemberInfo? member)
        {
            return member?.GetCustomAttribute<AlignmentAttribute>()?.Alignment;
        }

        public static ConditionInfo? GetConditionInfo(MemberInfo member)
        {
            var conditionAttribute = member.GetCustomAttribute<ConditionAttribute>();
            if (conditionAttribute == null)
                return null;

            return new ConditionInfo(conditionAttribute.FieldName, conditionAttribute.Comparer,
                conditionAttribute.Value);
        }

        private static Encoding GetEncoding(StringEncoding encoding)
        {
            return encoding switch
            {
                StringEncoding.Ascii => Encoding.ASCII,
                StringEncoding.Utf8 => Encoding.UTF8,
                StringEncoding.Utf16 => Encoding.Unicode,
                StringEncoding.Unicode => Encoding.Unicode,
                StringEncoding.Utf32 => Encoding.UTF32,
                StringEncoding.Sjis => Encoding.GetEncoding("Shift-JIS"),
                _ => throw new InvalidOperationException($"Unknown string encoding {encoding}.")
            };
        }

        private Func<ValueStorage, int> ResolveCalculateLengthAttributeMethod(CalculateLengthAttribute attribute)
        {
            (Type, string) cacheKey = (attribute.CalculationType, attribute.CalculationMethodName);
            if (_calculateMethodCache.TryGetValue(cacheKey, out Func<ValueStorage, int>? calculateMethod))
                return calculateMethod;

            if (!attribute.CalculationType.IsClass)
                throw new InvalidOperationException("Type needs to be a class.");

            MethodInfo method = attribute.CalculationType.GetMethod(attribute.CalculationMethodName) 
                                ?? throw new InvalidOperationException($"Class does not contain a method '{attribute.CalculationMethodName}'.");

            ParameterInfo[] methodParameters = method.GetParameters();
            if (method.ReturnType != typeof(int) ||
                methodParameters.Length != 1 ||
                !methodParameters[0].ParameterType.IsAssignableTo(typeof(ValueStorage)))
                throw new InvalidOperationException($"Method is not of form 'int {attribute.CalculationMethodName}({nameof(ValueStorage)})'.");

            if (attribute.CalculationType is { IsAbstract: true, IsSealed: true })
            {
                // If class is static
                return _calculateMethodCache[cacheKey] = storage => (int)method.Invoke(null, [storage])!;
            }

            // If class has to be instantiated
            if (attribute.CalculationType.GetConstructors().All(x => x.GetParameters().Length != 0))
                throw new InvalidOperationException("Class needs to have an empty constructor.");

            object? classInstance = Activator.CreateInstance(attribute.CalculationType);
            return _calculateMethodCache[cacheKey] = storage => (int)method.Invoke(classInstance, [storage])!;
        }
    }
}
