using FastMember;
using SAPbobsCOM;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SAPB1.DIAPI.Helper
{
    public static class UdoExtension
    {
        public static GeneralDataParams ConvertToParam(this IGeneralDataField source, GeneralService service,
            bool ignorePrimary = false)
        {
            GeneralDataParams generalDataParams = (GeneralDataParams)service
                .GetDataInterface(GeneralServiceDataInterfaces.gsGeneralDataParams);

            TypeAccessor typeAccessor = TypeAccessor.Create(source.GetType());

            foreach (Member member in typeAccessor.GetMembers())
            {
                SboFieldAttribute memberAttribute = member.GetMemberAttribute<SboFieldAttribute>();
                if (memberAttribute == null)
                    continue;

                object obj = typeAccessor[source, member.Name];
                if (obj == null)
                    continue;

                string name = memberAttribute.FieldName;
                if (!ignorePrimary)
                {
                    SboPrimaryKeyAttribute memberAttribute2 = member.GetMemberAttribute<SboPrimaryKeyAttribute>();
                    if (memberAttribute2 != null)
                    {
                        generalDataParams.SetProperty(name, obj);
                    }
                }
                else
                {
                    generalDataParams.SetProperty(name, obj);
                }
            }

            return generalDataParams;
        }

        public static void SetValue(this GeneralData generalData, IGeneralDataField source,
                bool ignorePrimaryKey = true)
        {
            var accessor = TypeAccessor.Create(source.GetType(), true);

            foreach (var member in accessor.GetMembers())
            {
                var fieldAttribute = member.GetMemberAttribute<SboFieldAttribute>();
                if (fieldAttribute == null)
                    continue;

                object value = accessor[source, member.Name];
                if (value == null)
                    continue;

                string fieldName = fieldAttribute.FieldName;

                var keyAttribute = member.GetMemberAttribute<SboPrimaryKeyAttribute>();
                if (keyAttribute != null)
                {
                    if (!ignorePrimaryKey)
                    {
                        generalData.SetProperty(fieldName, value);
                    }
                }
                else
                {
                    generalData.SetProperty(fieldName, value);
                }
            }
        }

        public static T MapValue<T>(this GeneralData source) where T : IGeneralDataField
        {
            var result = (T)Activator.CreateInstance(typeof(T));
            var accessor = TypeAccessor.Create(result.GetType());

            foreach (var member in accessor.GetMembers())
            {
                var fieldAttribute = member.GetMemberAttribute<SboFieldAttribute>();
                if (fieldAttribute == null)
                    continue;

                object value = source.GetProperty(fieldAttribute.FieldName);
                if (value == null)
                    continue;

                accessor[result, member.Name] = value;
            }

            return result;
        }

        public static UdoChildTable Item(this List<UdoChildTable> s, string name)
        {
            return s.Where(x => x.TableName == name).FirstOrDefault();
        }

        public static UdoChildTable Item(this List<UdoChildTable> s, int index)
        {
            return s.ElementAt(index);
        }
    }
}