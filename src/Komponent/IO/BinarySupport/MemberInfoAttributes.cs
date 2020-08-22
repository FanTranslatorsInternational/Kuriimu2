using System.Collections.Generic;
using System.Reflection;
using Komponent.IO.Attributes;
using Kontract;

namespace Komponent.IO.BinarySupport
{
    class MemberAttributeInfo
    {
        private readonly MemberInfo _member;

        public MemberAttributeInfo(MemberInfo member)
        {
            ContractAssertions.IsNotNull(member, nameof(member));

            _member = member;
        }

        public EndiannessAttribute EndiannessAttribute => _member.GetCustomAttribute<EndiannessAttribute>();
        public FixedLengthAttribute FixedLengthAttribute => _member.GetCustomAttribute<FixedLengthAttribute>();
        public VariableLengthAttribute VariableLengthAttribute => _member.GetCustomAttribute<VariableLengthAttribute>();
        public CalculateLengthAttribute CalculatedLengthAttribute => _member.GetCustomAttribute<CalculateLengthAttribute>();
        public BitFieldInfoAttribute BitFieldInfoAttribute => _member.GetCustomAttribute<BitFieldInfoAttribute>();
        public AlignmentAttribute AlignmentAttribute => _member.GetCustomAttribute<AlignmentAttribute>();
        public IEnumerable<TypeChoiceAttribute> TypeChoiceAttributes => _member.GetCustomAttributes<TypeChoiceAttribute>();
    }
}
