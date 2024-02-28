using SAPbobsCOM;
using System;
using System.Collections.Generic;

namespace SAPB1.DIAPI.Helper
{
    public interface ISboProvider : IDisposable
    {
        Company Company { get; }
        bool IsValidCompany { get; }
        bool Connect();
        void Disconnect();
        bool IsInTransaction { get; }
        void StartTransaction();
        void Commit();
        void Rollback();
        SboLastError GetLastError();
        T GetBusinessObject<T>(BoObjectTypes boObjectTypes);
        GeneralService GetGeneralService(string name);
        Version GetCOMVersion();
        List<T> SqlQuery<T>(string sql, bool manualColumnMapping = false);
        UdoProvider GetUDO(string name);
        string GetNewObjectCode();
        int GetNewObjectKey();
    }
}