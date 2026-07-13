using SAPbobsCOM;
using System;
using System.Collections.Generic;

namespace SAPB1.DIAPI.Helper
{
    public static class QueryExtension
    {
        public static Dictionary<string, object> ToList(this Fields fields)
        {
            var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            if (fields.Count == 0)
                return result;

            for (var i = 0; i < fields.Count; i++)
            {
                var field = fields.Item(i);
                result[field.Name] = field.Value;
            }

            return result;
        }

        public static T MapEntityValue<T>(this Fields fields, bool manualColumnMapping = false)
        {
            if (fields.Count == 0)
                return default(T);

            var map = MemberMapCache.Get(typeof(T));
            var accessor = map.Accessor;
            var result = (T)Activator.CreateInstance(typeof(T));
            var items = fields.ToList();

            foreach (var member in map.Members)
            {
                var fieldName = manualColumnMapping
                    ? GetMappedFieldName(map, member.Name)
                    : member.Name;

                if (string.IsNullOrWhiteSpace(fieldName))
                    continue;

                object value;
                if (items.TryGetValue(fieldName, out value))
                    accessor[result, member.Name] = value;
            }

            return result;
        }

        public static List<T> ToList<T>(this Recordset recordset,
            bool manualColumnMapping = false)
        {
            var result = new List<T>();

            if (recordset.RecordCount == 0)
                return result;

            recordset.MoveFirst();
            while (!recordset.EoF)
            {
                var value = recordset.Fields.MapEntityValue<T>(manualColumnMapping);

                result.Add(value);

                recordset.MoveNext();
            }

            return result;
        }

        private static string GetMappedFieldName(TypeMemberMap map, string memberName)
        {
            SboMemberMap member;
            return map.SboMembersByName.TryGetValue(memberName, out member)
                ? member.FieldName
                : string.Empty;
        }
    }
}
