using FastMember;
using SAPbobsCOM;
using System.Reflection;
using System;

namespace SAPB1.DIAPI.Helper
{
    internal static class RuntimeExtension
    {
        public static BoDataServerTypes ConvertToBoServerType(this SboServerType s)
        {
            BoDataServerTypes result;

            switch (s)
            {
                case SboServerType.SapMsSql2019:
                    result = BoDataServerTypes.dst_MSSQL2019;
                    break;
                case SboServerType.SapMsSql2017:
                    result = BoDataServerTypes.dst_MSSQL2017;
                    break;
                case SboServerType.SapMsSql2016:
                    result = BoDataServerTypes.dst_MSSQL2016;
                    break;
                case SboServerType.SapMsSql2014:
                    result = BoDataServerTypes.dst_MSSQL2014;
                    break;
                case SboServerType.SapMsSql2012:
                    result = BoDataServerTypes.dst_MSSQL2012;
                    break;
                case SboServerType.SapMsSql2008:
                    result = BoDataServerTypes.dst_MSSQL2008;
                    break;
                case SboServerType.SapMsSql2005:
                    result = BoDataServerTypes.dst_MSSQL2005;
                    break;
                case SboServerType.SapMsSql:
                    result = BoDataServerTypes.dst_MSSQL2005;
                    break;
                case SboServerType.SapMaxDb:
                    result = BoDataServerTypes.dst_MAXDB; 
                    break;
                case SboServerType.SapSysBase:
                    result = BoDataServerTypes.dst_SYBASE;
                    break;
                case SboServerType.SapDb2:
                    result = BoDataServerTypes.dst_DB_2;
                    break;
                default:
                    result = BoDataServerTypes.dst_HANADB;
                    break;
            }

            return result;
        }

        public static T GetPrivateField<T>(this object obj, string name)
        {
            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            Type type = obj.GetType();
            FieldInfo field = type.GetField(name, flags);

            return (T)field.GetValue(obj);
        }

        public static T GetMemberAttribute<T>(this Member member) where T : Attribute
        {
            return GetPrivateField<MemberInfo>(member, "member").GetCustomAttribute<T>(true);
        }

        public static string GetFieldName(this Member member)
        {
            var attribute = member.GetMemberAttribute<SboFieldAttribute>();

            return attribute?.FieldName ?? string.Empty;
        }
    }
}