using SAPbobsCOM;
using System.Collections.Generic;

namespace SAPB1.DIAPI.Helper
{
    public class UdoChildTable : ProviderBase
    {
        private string _tableName;
        private GeneralData _data;
        private GeneralDataCollection _dataCollections;

        public UdoChildTable(string tableName)
        {
            _tableName = tableName;
        }

        public string TableName => _tableName;

        public void SetGeneralData(GeneralData data)
        {
            _data = data;
            _dataCollections = _data.Child(_tableName);
        }

        public void AddList<T>(List<T> values) where T : GeneralDataRowField
        {
            if (values == null && values.Count == 0)
                return;

            foreach (T value in values)
                Add<T>(value);
        }

        public void Add<T>(T value) where T : GeneralDataRowField
        {
            if (value == null)
                return;

            _dataCollections.Add().SetValue(value);
        }

        public void RemoveAtLine(int lineNumber)
        {
            RemoveAt(lineNumber - 1);
        }

        public void RemoveAt(int index)
        {
            _dataCollections.Remove(index);
        }

        public void RemoveAll()
        {
            for (var i = 0; i < _dataCollections.Count; i++)
            {
                RemoveAt(i);
            }
        }

        public void UpdateList<T>(List<T> values) where T : GeneralDataRowField
        {
            if (values == null && values.Count == 0)
                return;

            foreach (T value in values)
                Update<T>(value);
        }

        public void Update<T>(T value) where T : GeneralDataRowField
        {
            if (value == null)
                return;

            _dataCollections.Item(value.LineId).SetValue(value);
        }

        public List<T> GetValues<T>() where T : GeneralDataRowField
        {
            var result = new List<T>();
            var items = _dataCollections;

            for (var i = 0; i < items.Count; i++)
                result.Add(items.Item(i).MapValue<T>());

            return result;
        }
    }
}