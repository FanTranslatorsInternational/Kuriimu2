using Komponent.IO.Attributes;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Komponent.IO
{
    internal class MemberAttributeInfo
    {
        MemberInfo _member;

        public MemberAttributeInfo(MemberInfo member)
        {
            _member = member ?? throw new ArgumentNullException(nameof(member));
        }

        public EndiannessAttribute EndiannessAttribute => _member.GetCustomAttribute<EndiannessAttribute>();
        public FixedLengthAttribute FixedLengthAttribute => _member.GetCustomAttribute<FixedLengthAttribute>();
        public VariableLengthAttribute VariableLengthAttribute => _member.GetCustomAttribute<VariableLengthAttribute>();
        public BitFieldInfoAttribute BitFieldInfoAttribute => _member.GetCustomAttribute<BitFieldInfoAttribute>();
        public AlignmentAttribute AlignmentAttribute => _member.GetCustomAttribute<AlignmentAttribute>();
        public IEnumerable<TypeChoiceAttribute> TypeChoiceAttributes => _member.GetCustomAttributes<TypeChoiceAttribute>();
    }
}