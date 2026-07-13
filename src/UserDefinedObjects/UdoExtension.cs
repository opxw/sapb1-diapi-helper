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

            var map = MemberMapCache.Get(source.GetType());

            foreach (var member in map.SboMembers)
            {
                object value = map.Accessor[source, member.MemberName];
                if (value == null)
                    continue;

                if (ignorePrimary || member.IsPrimaryKey)
                    generalDataParams.SetProperty(member.FieldName, value);
            }

            return generalDataParams;
        }

        public static void SetValue(this GeneralData generalData, IGeneralDataField source,
                bool ignorePrimaryKey = true)
        {
            var map = MemberMapCache.Get(source.GetType());

            foreach (var member in map.SboMembers)
            {
                object value = map.Accessor[source, member.MemberName];
                if (value == null)
                    continue;

                if (!member.IsPrimaryKey || !ignorePrimaryKey)
                    generalData.SetProperty(member.FieldName, value);
            }
        }

        public static T MapValue<T>(this GeneralData source) where T : IGeneralDataField
        {
            var result = (T)Activator.CreateInstance(typeof(T));
            var map = MemberMapCache.Get(result.GetType());

            foreach (var member in map.SboMembers)
            {
                object value = source.GetProperty(member.FieldName);
                if (value == null)
                    continue;

                map.Accessor[result, member.MemberName] = value;
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
