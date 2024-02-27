namespace SAPB1.DIAPI.Helper
{
    public enum SboServerType
    {
        SapMsSql2019 = 0,
        SapMsSql2017,
        SapMsSql2016,
        SapMsSql2014,
        SapMsSql2012,
        SapMsSql2008,
        SapMsSql2005,
        SapMsSql,
        SapHanaDb,
        SapMaxDb,
        SapSysBase,
        SapDb2
    }

    public enum SboRecordsetFillParam
    {
        UseDefinedValues,
        FillIntoValues,
    }
}