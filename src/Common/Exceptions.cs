using System;

namespace SAPB1.DIAPI.Helper
{
    public class SboInvalidCompanyException : Exception
    {
        public override string Message => "Invalid Company object";
    }

    public class SboInvalidValueException : Exception
    {
        public override string Message => "Invalid value (value not set)";
    }
}