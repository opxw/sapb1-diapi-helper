using FastMember;
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
            var accessor = TypeAccessor.Create(source.GetType());

            foreach (var member in accessor.GetMembers())
            {
                var field = member.GetFieldName();
                if (!string.IsNullOrWhiteSpace(field))
                {
                    var value = accessor[source, member.Name];
                    if (value != null)
                        fields.Item(field).Value = value;
                }
            }
        }
    }
}