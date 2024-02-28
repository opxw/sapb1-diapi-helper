using SAPbobsCOM;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

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

            if (!IsInTransaction)
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
            Company.CompanyDB = _configuration.CompanyDatabase;
            Company.UseTrusted = true;
            Company.Connect();

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

        public Version GetCOMVersion()
        {
            Assembly assembly = null;

            try
            {
                Assembly.LoadFrom("Interop.SAPbobsCOM.dll");
            }
            catch (Exception ex)
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

            recordset.DoQuery(sql);

            return recordset.ToList<T>();
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
                    Marshal.FinalReleaseComObject(Company);
            }

            base.Dispose(disposing);
        }

    }
}