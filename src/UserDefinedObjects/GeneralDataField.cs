namespace SAPB1.DIAPI.Helper
{
    public class GeneralDataField : IGeneralDataField
    {
        [SboField("DocEntry"), SboPrimaryKey]
        public int? DocEntry { get; set; }
    }
}