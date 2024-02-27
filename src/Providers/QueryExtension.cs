﻿using FastMember;
using SAPbobsCOM;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SAPB1.DIAPI.Helper
{
    public static class QueryExtension
    {
        private static Dictionary<string, object> ToList(this Fields fields)
        {
            var result = new Dictionary<string, object>();

            if (fields.Count == 0)
                goto Result;

            for (var i = 0; i < fields.Count; i++)
            {
                var field = fields.Item(i);
                result.Add(field.Name, field.Value);
            }

        Result:
            return result;
        }

        public static T MapEntityValue<T>(this Fields fields, bool manualColumnMapping = false)
        {
            if (fields.Count == 0)
                return default(T);

            var accessor = TypeAccessor.Create(typeof(T), true);
            var result = (T)Activator.CreateInstance(typeof(T));
            var members = accessor.GetMembers();
            var items = fields.ToList();

            foreach (var member in members)
            {
                var fieldName = manualColumnMapping ? member.GetFieldName() : member.Name;

                if (!string.IsNullOrWhiteSpace(fieldName))
                {
                    var value = items.Where(x => x.Key.Equals(fieldName, StringComparison.OrdinalIgnoreCase))
                        .FirstOrDefault().Value;

                    accessor[result, member.Name] = value;
                }
            }

            return result;
        }
    }
}