using SAPbobsCOM;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

namespace SAPB1.DIAPI.Helper
{
    public class SboProvider : ProviderBase, ISboProvider
    {
        private readonly SboConfiguration _configuration;

        public SboProvider(SboConfiguration configuration)
        {
            _configuration = configuration;
        }

        public Company Company { get; private set; } = new Company();

        public bool IsValidCompany => Company != null && Company.Connected;

        public bool IsInTransaction
        {
            get
            {
                CheckForValidCompany();
                return Company.InTransaction;
            }
        }

        private void CheckForValidCompany()
        {
            if (!IsValidCompany)
                throw new SboInvalidCompanyException();
        }

        public void Commit()
        {
            CheckForValidCompany();

            if (IsInTransaction)
                Company.EndTransaction(BoWfTransOpt.wf_Commit);
        }

        public bool Connect()
        {
            Company.DbServerType = _configuration.ServerType.ConvertToBoServerType();
            Company.Server = _configuration.Server;
            Company.LicenseServer = _configuration.LicenseServer;
            Company.SLDServer = _configuration.SLDServer;
            Company.UserName = _configuration.User;
            Company.Password = _configuration.Password;
            Company.DbUserName = _configuration.DatabaseUser;
            Company.DbPassword = _configuration.DatabasePassword;
            Company.CompanyDB = _configuration.CompanyDatabase;
            Company.UseTrusted = _configuration.Trusted;
            ConnectCompany();

            return Company.Connected;
        }

        public void Disconnect()
        {
            CheckForValidCompany();

            Company.Disconnect();
        }

        public T GetBusinessObject<T>(BoObjectTypes boObjectTypes)
        {
            CheckForValidCompany();

            var result = default(T);
            var boObject = Company.GetBusinessObject(boObjectTypes);

            if (boObject != null)
                result = (T)boObject;

            return result;
        }

        public T GetBusinessService<T>(ServiceTypes serviceTypes)
        {
            CheckForValidCompany();

            var result = default(T);
            var service = Company.GetCompanyService()
                .GetBusinessService(serviceTypes);

            if (service != null)
                result = (T)service;

            return result;
        }

        public Version GetCOMVersion()
        {
            Assembly assembly = null;

            try
            {
                assembly = Assembly.LoadFrom("Interop.SAPbobsCOM.dll");
            }
            catch
            {
            }

            if (assembly != null)
                return assembly.GetName().Version;
            else
                return null;
        }

        public GeneralService GetGeneralService(string name)
        {
            CheckForValidCompany();

            return Company.GetCompanyService()
                .GetGeneralService(name);
        }

        public SboLastError GetLastError()
        {
            var errorCode = Company.GetLastErrorCode();

            SboLastError error = null;
            if (errorCode != 0)
            {
                error = new SboLastError()
                {
                    Code = errorCode,
                    Description = Company.GetLastErrorDescription()
                };
            }

            return error;
        }

        public UdoProvider GetUDO(string name)
        {
            return new UdoProvider(this, name);
        }

        public void Rollback()
        {
            CheckForValidCompany();

            if (IsInTransaction)
                Company.EndTransaction(BoWfTransOpt.wf_RollBack);
        }

        public List<T> SqlQuery<T>(string sql, bool manualColumnMapping = false)
        {
            var recordset = GetBusinessObject<Recordset>(BoObjectTypes.BoRecordset);

            try
            {
                recordset.DoQuery(sql);

                return recordset.ToList<T>(manualColumnMapping);
            }
            finally
            {
                ReleaseComObject(recordset);
            }
        }

        public dynamic SqlScalarQuery(string sql)
        {
            var recordset = GetBusinessObject<Recordset>(BoObjectTypes.BoRecordset);

            try
            {
                recordset.DoQuery(sql);

                return recordset.Fields.Item(0).Value;
            }
            finally
            {
                ReleaseComObject(recordset);
            }
        }

        public void StartTransaction()
        {
            CheckForValidCompany();

            Company.StartTransaction();
        }

        public string GetNewObjectCode()
        {
            CheckForValidCompany();

            var result = string.Empty;
            Company.GetNewObjectCode(out result);

            return result;
        }

        public int GetNewObjectKey()
        {
            CheckForValidCompany();

            var result = Company.GetNewObjectKey();

            return string.IsNullOrWhiteSpace(result)
                ? 0
                : Convert.ToInt32(result);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (Company != null)
                    ReleaseComObject(Company);
            }

            base.Dispose(disposing);
        }

        private static void ReleaseComObject(object value)
        {
            if (value != null && Marshal.IsComObject(value))
                Marshal.FinalReleaseComObject(value);
        }

        private void ConnectCompany()
        {
            if (!_configuration.UseLoginGate)
            {
                Company.Connect();
                return;
            }

            var gateName = string.IsNullOrWhiteSpace(_configuration.LoginGateName)
                ? @"Global\SAPB1_DIAPI_LOGIN_GATE"
                : _configuration.LoginGateName;
            var timeout = _configuration.LoginGateTimeout <= TimeSpan.Zero
                ? TimeSpan.FromSeconds(120)
                : _configuration.LoginGateTimeout;

            using (var loginGate = new Mutex(false, gateName))
            {
                var hasLock = false;
                try
                {
                    try
                    {
                        hasLock = loginGate.WaitOne(timeout);
                    }
                    catch (AbandonedMutexException)
                    {
                        hasLock = true;
                    }

                    if (!hasLock)
                        throw new SboLoginGateTimeoutException(gateName, timeout);

                    Company.Connect();
                }
                finally
                {
                    if (hasLock)
                        loginGate.ReleaseMutex();
                }
            }
        }
    }
}
