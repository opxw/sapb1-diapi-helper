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

    public class SboLoginGateTimeoutException : TimeoutException
    {
        public SboLoginGateTimeoutException(string gateName, TimeSpan timeout)
            : base($"Timeout waiting for SAP Business One DI API login gate '{gateName}' after {timeout.TotalSeconds:0} seconds")
        {
            GateName = gateName;
            Timeout = timeout;
        }

        public string GateName { get; }
        public TimeSpan Timeout { get; }
    }
}
