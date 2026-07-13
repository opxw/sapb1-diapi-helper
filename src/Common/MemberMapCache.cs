using FastMember;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;

namespace SAPB1.DIAPI.Helper
{
    internal sealed class SboMemberMap
    {
        public SboMemberMap(string memberName, string fieldName, bool isPrimaryKey)
        {
            MemberName = memberName;
            FieldName = fieldName;
            IsPrimaryKey = isPrimaryKey;
        }

        public string MemberName { get; }
        public string FieldName { get; }
        public bool IsPrimaryKey { get; }
    }

    internal sealed class TypeMemberMap
    {
        public TypeMemberMap(TypeAccessor accessor, IReadOnlyList<Member> members, IReadOnlyList<SboMemberMap> sboMembers, IReadOnlyDictionary<string, SboMemberMap> sboMembersByName)
        {
            Accessor = accessor;
            Members = members;
            SboMembers = sboMembers;
            SboMembersByName = sboMembersByName;
        }

        public TypeAccessor Accessor { get; }
        public IReadOnlyList<Member> Members { get; }
        public IReadOnlyList<SboMemberMap> SboMembers { get; }
        public IReadOnlyDictionary<string, SboMemberMap> SboMembersByName { get; }
    }

    internal static class MemberMapCache
    {
        private static readonly ConcurrentDictionary<Type, TypeMemberMap> Cache = new ConcurrentDictionary<Type, TypeMemberMap>();

        public static TypeMemberMap Get(Type type)
        {
            return Cache.GetOrAdd(type, CreateMap);
        }

        private static TypeMemberMap CreateMap(Type type)
        {
            var accessor = TypeAccessor.Create(type, true);
            var members = new List<Member>();
            var sboMembers = new List<SboMemberMap>();
            var sboMembersByName = new Dictionary<string, SboMemberMap>(StringComparer.Ordinal);

            foreach (var member in accessor.GetMembers())
            {
                members.Add(member);

                var memberInfo = member.GetPrivateField<MemberInfo>("member");
                var fieldAttribute = memberInfo.GetCustomAttribute<SboFieldAttribute>(true);
                if (fieldAttribute == null)
                    continue;

                var primaryKeyAttribute = memberInfo.GetCustomAttribute<SboPrimaryKeyAttribute>(true);
                var sboMember = new SboMemberMap(member.Name, fieldAttribute.FieldName, primaryKeyAttribute != null);
                sboMembers.Add(sboMember);
                sboMembersByName[member.Name] = sboMember;
            }

            return new TypeMemberMap(accessor, members, sboMembers, sboMembersByName);
        }
    }
}
