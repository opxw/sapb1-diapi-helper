using SAPbobsCOM;
using System.Collections.Generic;
using System.Linq;

namespace SAPB1.DIAPI.Helper
{
    public class UdoProvider : ProviderBase
    {
        private string _objectName;
        private ISboProvider _provider;
        private GeneralService _service;
        private GeneralData _data;
        private UdoProperties _properties;
        private List<UdoChildTable> _childTables;
        
        public GeneralDataField Values { get; set; }

        public UdoProperties Properties => _properties;

        public UdoProvider(ISboProvider provider, string objectName)
        {
            _provider = provider;
            _objectName = objectName;
            _service = _provider.GetGeneralService(_objectName);
            _data = _service.GetDataInterface(GeneralServiceDataInterfaces.gsGeneralData);
            _properties = GetProperties();
            _childTables = GetChildTables();

            SetDataForChilds();
        }

        private void SetDataForChilds()
        {
            foreach (var child in _childTables)
            {
                child.SetGeneralData(_data);
            }
        }

        public void GetByParams<T>(GeneralDataField parameters, 
            SboRecordsetFillParam recordFill = SboRecordsetFillParam.FillIntoValues) where T : GeneralDataField
        {
            _data = _service.GetByParams(parameters.ConvertToParam(_service));

            if (recordFill == SboRecordsetFillParam.FillIntoValues)
            {
                Values = GetValues<T>();
            }

            foreach (var child in  _childTables)
            {
                child.SetGeneralData(_data);
            }
        }

        private UdoProperties GetProperties()
        {
            var result = default(UdoProperties);

            var queryRecord = _provider.SqlQuery<UdoProperties>(
                @" SELECT Code, Name, TableName, LogTable, Type " +
                 " FROM   OUDO " +
                 " WHERE  Code = '" + _objectName + "'"
                );

            if (queryRecord.Count > 0)
                result = queryRecord.FirstOrDefault();

            return result;
        }

        private List<UdoChildTable> GetChildTables()
        {
            var result = new List<UdoChildTable>();

            if (string.IsNullOrWhiteSpace(_objectName))
                return result;

            var queryRecord = _provider.SqlQuery<UdoChildRecord>(
                @" SELECT   Code, TableName, SonNum AS Num " +
                 " FROM     UDO1 " +
                 " WHERE    Code = '" + _objectName + "'" +
                 " ORDER BY SonNum "
                );

            if (queryRecord.Count > 0)
            {
                foreach (var record in queryRecord)
                    result.Add(new UdoChildTable(record.TableName));
            }

            return result;
        }

        private T GetValues<T>() where T : GeneralDataField
        {
            var result = default(T);

            if (_data != null)
                result = _data.MapValue<T>();

            return result;
        }

        public string ObjectName => _objectName;

        public List<UdoChildTable> ChildTables => _childTables;

        public object Insert()
        {
            object result = null;

            _data.SetValue(Values, false);
            var entry = _service.Add(_data);

            if (_properties.Type == "1")
                result = entry.GetProperty("Code");
            else
                result = entry.GetProperty("DocEntry");

            return result;
        }

        public void Update()
        {
            _data.SetValue(Values);
            _service.Update(_data);
        }

        public void Cancel()
        {
            _service.Cancel(Values.ConvertToParam(_service));
        }

        public void Close()
        {
            _service.Close(Values.ConvertToParam(_service));
        }

        public void Delete()
        {
            _service.Delete(Values.ConvertToParam(_service));
        }

        public void Reset()
        {
            Values = null;
            _data = null;
            _data = _service.GetDataInterface(GeneralServiceDataInterfaces.gsGeneralData);

            foreach (var table in ChildTables)
                table.SetGeneralData(_data);
        }
    }
}