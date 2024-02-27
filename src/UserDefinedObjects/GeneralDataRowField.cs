namespace SAPB1.DIAPI.Helper
{
    public class GeneralDataRowField : IGeneralDataField
    {
        [SboField("LineId"), SboPrimaryKey]
        public int? LineId { get; set; }
    }
}