# SAP BusinessOne Data Interface API

A Wrapper of DIAPI with more extras such as UDF field mapping, easy access to UDO, SQL query, and more.

## Requirements

DIAPI must be installed on machine (tested on DIAPI 9 & 10 SAP HANA & SQL Server version) & Add COM reference into your C# project.

### Defining Connection

```C#
var provider = new SboProvider(new SboConfiguration()
{
    ServerType = SboServerType.SapMsSql2019,
    Server = "SERVER",
    CompanyDatabase = "ACME_DB",
    LicenseServer = "SERVER:30000",
    User = "johndoe",
    Password = "password"
});
var connected = provider.Connect();
```

### Accessing Business Object

```C#
// get business partner master data
var businessPartner = provider.GetBusinessObject<BusinessPartners>
    (BoObjectTypes.oBusinessPartners);

// get goods receipt
var goodsReceipt = provider.GetBusinessObject<Documents>
    (BoObjectTypes.oInventoryGenEntry);
```

### UDF Field Mapping

```C#
Assign(IUdf source);
```

This method is applied to `UserFields` data type.

Example of case :

![c](https://private-user-images.githubusercontent.com/146586887/309168891-8e0f1cfc-edaf-4c53-8c49-e0a225d85f34.png?jwt=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJnaXRodWIuY29tIiwiYXVkIjoicmF3LmdpdGh1YnVzZXJjb250ZW50LmNvbSIsImtleSI6ImtleTUiLCJleHAiOjE3MDkyNzU3MDgsIm5iZiI6MTcwOTI3NTQwOCwicGF0aCI6Ii8xNDY1ODY4ODcvMzA5MTY4ODkxLThlMGYxY2ZjLWVkYWYtNGM1My04YzQ5LWUwYTIyNWQ4NWYzNC5wbmc_WC1BbXotQWxnb3JpdGhtPUFXUzQtSE1BQy1TSEEyNTYmWC1BbXotQ3JlZGVudGlhbD1BS0lBVkNPRFlMU0E1M1BRSzRaQSUyRjIwMjQwMzAxJTJGdXMtZWFzdC0xJTJGczMlMkZhd3M0X3JlcXVlc3QmWC1BbXotRGF0ZT0yMDI0MDMwMVQwNjQzMjhaJlgtQW16LUV4cGlyZXM9MzAwJlgtQW16LVNpZ25hdHVyZT02ZTQzZjQwMDMzODBlMDcyZjc5NThlYzMxNjBjNzUxYzMxZjk2M2M3Zjk3NGMxYWI3OTY2OWNmZTU2YjQyNTMyJlgtQW16LVNpZ25lZEhlYWRlcnM9aG9zdCZhY3Rvcl9pZD0wJmtleV9pZD0wJnJlcG9faWQ9MCJ9.cJjYxn4mi0Ce_7MdlbZg5F3E4R3lrMysEoKu6crqVfs)

Marketing Document in SAP B1 is any document relating to the planning, pricing, coordination, promotion, purchase, sale, or distribution of goods or services. So the UDF will be placed on related transaction interfaces e.g. SalesOrder, GoodsReceipt, Inventory Transfer, Goods Issue or Receipt, etc. 

Now, we will manage those fields from *Marketing Documents* UDF into POCO class like this:

```C#
public class MarketingDocumentUdf : IUdf
{
    [SboField("U_AnotherDate")]
    public DateTime? AnotherDate { get; set; }

    [SboField("U_AnotherNumeric")]
    public int? AnotherNumber { get; set; }

    [SboField("U_AnotherText")]
    public string? AnotherText { get; set; }
}

public class MarketingDocumentRowUdf : IUdf
{
    [SboField("U_TestRow1")]
    public string? TestRow { get; set; }
}
```

And below is how we implement those fields into the transaction

```C#
var docUdf = new MarketingDocumentUdf()
{
    AnotherDate = DateTime.Today.AddDays(1),
    AnotherNumber = 1000,
    AnotherText = "test",
};

var goodsIssue = provider.GetBusinessObject<Documents>
    (BoObjectTypes.oInventoryGenExit);
goodsIssue.UserFields.Assign(docUdf);

var goodsReceipt = provider.GetBusinessObject<Documents>
    (BoObjectTypes.oInventoryGenEntry);
goodsReceipt.UserFields.Assign(docUdf);
goodsReceipt.Lines.UserFields.Assign(new MarketingDocumentRowUdf()
{
    TestRow = "value"
});
```

As we can see, the variable`docUdf` is reusable and we can use into another document transaction.

Now, let's compare with native (origin) from DIAPI SDK:

```C#
var goodsIssue = (Documents)
company.GetBusinessObject(BoObjectTypes.oInventoryGenEntry);
goodsIssue.UserFields.Fields.Item("U_AnotherDate").Value = DateTime.Today.AddDays(1);
goodsIssue.UserFields.Fields.Item("U_AnotherNumeric").Value = 1000;
goodsIssue.UserFields.Fields.Item("U_AnotherText").Value = "test";

var goodsReceipt = (Documents)
company.GetBusinessObject(BoObjectTypes.oInventoryGenEntry);
goodsReceipt.UserFields.Fields.Item("U_AnotherDate").Value = DateTime.Today.AddDays(1);
goodsReceipt.UserFields.Fields.Item("U_AnotherNumeric").Value = 1000;
goodsReceipt.UserFields.Fields.Item("U_AnotherText").Value = "test";
goodsReceipt.Lines.UserFields.Fields.Item("U_TestRow1").Value = "row value";

// we need more effort to save GoodsIssue & Receipt documents at same time
// it's just about setting up the value of fields
```

> You must set all property data type (has getter & setter) as `Nullable`.
> 
> In .NET Framework : `public Nullable<int> Value { get; set; }`
> 
> In .NET Core, simply put `?` sign after data type. 
> 
> `NULL` value of the property will be ignored, so we can insert/update data partially.

### UDO PROVIDER

#### Calling or Get UDO

```c#
var udo = provider.GetUDO("REGISTERED_OBJECT_NAME");
```

#### Read Information Of UDO

```C#
UdoProperties properties = udo.Properties;

// here's the UdoProperties structure
class UdoProperties
{
    public string Code { get; set; }
    public string Name { get; set; }
    public string TableName { get; set; }
    public string LogTable { get; set; }
    // UDO type, 1 = Master Data otherwise Document
    public string Type { get; set; }
}
```

#### Child Tables

```C#
List<UdoChildTable> childs = udo.ChildTables;
```

- ##### Accessing The Child Table Object
  
  ```C#
  var table1 = childs.Item(0);
  or
  var table1 = childs.Item("NAME_OF_CHILD_TABLE");
  ```

- ##### Accessing Values
  
  ```C#
  List<T> GetValues<T>() where T : GeneralDataRowField
  
  var values = table1.GetValues<YourCustomType>(); 
  ```

- ##### Modify, Add or Delete Item on Child
  
  ```C#
  //add to existing GeneralDataCollection
  void AddList<T>(List<T> values) where T : GeneralDataRowField
  void Add<T>(T value) where T : GeneralDataRowField
  
  //update existing GeneralData in GeneralDataCollection
  UpdateList<T>(List<T> values) where T : GeneralDataRowField
  void Update<T>(T value) where T : GeneralDataRowField
  
  //remove existing GeneralData in GeneralDataCollection
  void RemoveAtLine(int lineId)
  void RemoveAll()
  ```

#### Insert/Update/Cancel/Close/Delete

```C#
// it will returning DocEntry if UDO type is Document otherwise Code if Master Data
object Insert();

public void Update();

// cancel & close only available if UDO type is Document
public void Cancel();
public void Close();

public void Delete();
```

---

Let's try it on available *User Table* `Item Sale` & has been registered as UDO named `ITM_SALE` 

<img title="" src="https://private-user-images.githubusercontent.com/146586887/309168915-851c8e4d-1ae0-4878-83e9-1f56405d637b.png?jwt=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJnaXRodWIuY29tIiwiYXVkIjoicmF3LmdpdGh1YnVzZXJjb250ZW50LmNvbSIsImtleSI6ImtleTUiLCJleHAiOjE3MDkyNzU3MDgsIm5iZiI6MTcwOTI3NTQwOCwicGF0aCI6Ii8xNDY1ODY4ODcvMzA5MTY4OTE1LTg1MWM4ZTRkLTFhZTAtNDg3OC04M2U5LTFmNTY0MDVkNjM3Yi5wbmc_WC1BbXotQWxnb3JpdGhtPUFXUzQtSE1BQy1TSEEyNTYmWC1BbXotQ3JlZGVudGlhbD1BS0lBVkNPRFlMU0E1M1BRSzRaQSUyRjIwMjQwMzAxJTJGdXMtZWFzdC0xJTJGczMlMkZhd3M0X3JlcXVlc3QmWC1BbXotRGF0ZT0yMDI0MDMwMVQwNjQzMjhaJlgtQW16LUV4cGlyZXM9MzAwJlgtQW16LVNpZ25hdHVyZT1lYTUxN2U5ZjBjNGE1NjgxYTFiZThjMDgyMzNkYTQzZDE2YWFlOWY2NDc4MzMzZWVjYjJjMWVmYmQyYWRmZTU2JlgtQW16LVNpZ25lZEhlYWRlcnM9aG9zdCZhY3Rvcl9pZD0wJmtleV9pZD0wJnJlcG9faWQ9MCJ9.CVXgh2Pj2QWqZZ9Xv09ppmpXKIGw6yk7Idc2KwZCr4k" alt="A" data-align="inline">

The first step, define those fields into POCO class:

```C#
ublic class ItemSale : GeneralDataField
{
    // this is system field & defined as key
    [SboField("Code"), SboPrimaryKey]
    public string? Code { get; set; }

    [SboField("U_ItemCode")]
    public string? ItemCode { get; set; }

    [SboField("U_Notes")]
    public string? Notes { get; set; }
}

public class ItemSaleDiscount : GeneralDataRowField
{
    [SboField("U_FromDate")]
    public DateTime? StartDate { get; set; }

    [SboField("U_Days")]
    public int? AvailableDays { get; set; }

    [SboField("U_Disc")]
    public double? Discount { get; set; }

    public ItemSaleDiscount(DateTime? startDate, 
        int? availableDays, 
        double? discount,
        int? lineId = null)
    {
        StartDate = startDate;
        AvailableDays = availableDays;
        Discount = discount;
        LineId = lineId;
    }
}
```

#### Insert

```C#
 // header
udo.Values = new ItemSale()
{
    Code = "MyCode",
    ItemCode = "123",
    Notes = "test"
 };

var discTable = udo.ChildTables.Item("ITM_SALE_DISC");

// add multiple values
discTable.AddList(new List<ItemSaleDiscount>()
{
    new ItemSaleDiscount(DateTime.Now, 2, 35),
    new ItemSaleDiscount(DateTime.Now.AddDays(1), 2, 30),
    new ItemSaleDiscount(DateTime.Now.AddDays(1), 2, 20),
});

// add single value
discTable.Add(new ItemSaleDiscount(DateTime.Now, 2, 35));

udo.Insert();
```

#### Update

```C#
udo.GetByParams<ItemSale>(new ItemSale()
{
    Code = "MyCode"
});
var discTable = udo.ChildTables.Item("ITM_SALE_DISC");

// update multiple values in line 1 & 3
discTable.UpdateList(new List<ItemSaleDiscount>()
{
    new ItemSaleDiscount(null, null, 25, 1),
    new ItemSaleDiscount(null, null, 25, 3)
});

// update value in line 1
discTable.Update(new ItemSaleDiscount(null, null, 25, 1));

udo.Update();
```

#### Read

```C#
udo.GetByParams<ItemSale>(new ItemSale()
{
    Code = "MyCode"
}, SboRecordsetFillParam.FillIntoValues);
```

### SQL Query

Examples :

With manual field mapping (using attribute) :

```C#
public class BusinessPartner
{
    [SboField("CardCode")]
    public string Code { get; set; }

    [SboField("CardName")]
    public string Name { get; set; }
}

var sql = "SELECT CardCode, CardName from OCRD";
var result = provider.SqlQuery<BusinessPartner>(sql, true);

// it will return List<BusinessPartner>
```

or, using automatic field mapping:

```C#
public class BusinessPartner
{
    public string Code { get; set; }
    public string Name { get; set; }
}

var sql = "SELECT CardCode AS Code, CardName from OCRD AS Name";
var result = provider.SqlQuery<BusinessPartner>(sql);
```
