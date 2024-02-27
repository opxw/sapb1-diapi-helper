using System;

namespace SAPB1.DIAPI.Helper
{
    [AttributeUsage(AttributeTargets.Property)]
    public class SboFieldAttribute : Attribute
    {
        public SboFieldAttribute(string fieldName)
        {
            FieldName = fieldName;
        }

        public string FieldName { get; set; }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class SboPrimaryKeyAttribute : Attribute
    {
    }
}