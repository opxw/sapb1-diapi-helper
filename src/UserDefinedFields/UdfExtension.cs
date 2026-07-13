using SAPbobsCOM;

namespace SAPB1.DIAPI.Helper
{
    public static class UdfExtension
    {
        public static void Assign(this UserFields userFields, IUdf source)
        {
            userFields.Fields.Assign(source);
        }

        public static void Assign(this Fields fields, IUdf source)
        {
            var map = MemberMapCache.Get(source.GetType());

            foreach (var member in map.SboMembers)
            {
                var value = map.Accessor[source, member.MemberName];
                if (value != null)
                    fields.Item(member.FieldName).Value = value;
            }
        }
    }
}
