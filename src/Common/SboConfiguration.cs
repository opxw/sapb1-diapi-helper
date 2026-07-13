using System;

namespace SAPB1.DIAPI.Helper
{
    public class SboConfiguration
    {
        public SboConfiguration()
        {
            UseLoginGate = true;
            LoginGateTimeout = TimeSpan.FromSeconds(120);
            LoginGateName = @"Global\SAPB1_DIAPI_LOGIN_GATE";
        }

        public SboServerType ServerType { get; set; }
        public string Server { get; set; }
        public string LicenseServer { get; set; }
        public string SLDServer { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
        public string DatabaseUser { get; set; }
        public string DatabasePassword { get; set; }
        public string CompanyDatabase { get; set; }
        public bool Trusted { get; set; }
        public bool UseLoginGate { get; set; }
        public TimeSpan LoginGateTimeout { get; set; }
        public string LoginGateName { get; set; }
    }
}
