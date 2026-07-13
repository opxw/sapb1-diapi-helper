# SAP Business One DI API Helper

[![NuGet version (SAPB1.DIAPI.Helper)](https://img.shields.io/nuget/v/SAPB1.DIAPI.Helper.svg?style=flat-square)](https://www.nuget.org/packages/SAPB1.DIAPI.Helper/)

`SAPB1.DIAPI.Helper` is a lightweight C# helper library for SAP Business One DI API (`SAPbobsCOM`). It provides a cleaner API for common DI API work such as connection management, UDF mapping, UDO access, SQL result mapping, and safer cross-process login handling.

## Features

- SAP Business One DI API connection wrapper.
- SQL query mapping into POCO classes.
- User-defined field (UDF) mapping through attributes.
- User-defined object (UDO) CRUD helper.
- UDO child table helpers.
- Optional database credentials for `SBO-COMMON` access.
- Cross-process login gate to reduce DI API license/SLD contention.
- Metadata caching for faster reflection-based mapping.
- Diagnostic script for DNS, network, SQL, SLD, license, COM load, and process startup checks.

## Supported Frameworks

This package targets Windows-only modern .NET runtimes because SAP DI API is COM-based:

```xml
net7.0-windows
net8.0-windows
net9.0-windows
net10.0-windows
```

## Requirements

- SAP Business One DI API installed on the client machine.
- Matching DI API version and architecture for your SAP Business One environment.
- Windows machine with registered `SAPbobsCOM` COM components.
- Visual Studio MSBuild / .NET Framework MSBuild for building projects with `COMReference`.

> `dotnet build` may fail with `ResolveComReference` errors for this project. Use Visual Studio MSBuild when building from source.

## Recommended Project Settings

For SAP Business One 10 DI API, x64 is usually the safest target:

```xml
<PropertyGroup>
  <PlatformTarget>x64</PlatformTarget>
  <Prefer32Bit>false</Prefer32Bit>
</PropertyGroup>
```

## Connection Example

```csharp
using SAPB1.DIAPI.Helper;

var provider = new SboProvider(new SboConfiguration
{
    ServerType = SboServerType.SapMsSql2019,
    Server = "YOUR-SAP-SERVER",
    LicenseServer = "YOUR-SAP-SERVER:30000",
    SLDServer = "YOUR-SAP-SERVER:40000",
    CompanyDatabase = "COMPANY_DB",
    User = "sap-user",
    Password = "sap-user-password",
    DatabaseUser = "db-user",
    DatabasePassword = "database-password",
    Trusted = false
});

var connected = provider.Connect();
```

When SLD or the license server returns an internal host name, make sure the client can resolve it. For example, in `C:\Windows\System32\drivers\etc\hosts`:

```text
192.0.2.10 YOUR-SAP-SERVER
```

## Login Gate

SAP DI API login can become slow or unstable when many executables call `Company.Connect()` at the same time, especially when all of them use the same SAP Business One user.

By default, this library uses a global named mutex before `Company.Connect()`:

```csharp
UseLoginGate = true;
LoginGateTimeout = TimeSpan.FromSeconds(120);
LoginGateName = @"Global\SAPB1_DIAPI_LOGIN_GATE";
```

This makes login attempts wait in line across processes. The lock is released immediately after `Connect()` succeeds or fails. Business logic and `Disconnect()` are not blocked by the gate.

You can disable it if you already have an external queue or scheduler:

```csharp
var config = new SboConfiguration
{
    UseLoginGate = false
};
```

## Accessing Business Objects

```csharp
var businessPartner = provider.GetBusinessObject<BusinessPartners>(
    BoObjectTypes.oBusinessPartners);

var goodsReceipt = provider.GetBusinessObject<Documents>(
    BoObjectTypes.oInventoryGenEntry);
```

## UDF Mapping

Create a POCO class and map each property to a SAP Business One UDF field using `[SboField]`.

```csharp
public class MarketingDocumentUdf : IUdf
{
    [SboField("U_AnotherDate")]
    public DateTime? AnotherDate { get; set; }

    [SboField("U_AnotherNumber")]
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

Assign the values to DI API objects:

```csharp
var udf = new MarketingDocumentUdf
{
    AnotherDate = DateTime.Today.AddDays(1),
    AnotherNumber = 1000,
    AnotherText = "test"
};

var goodsIssue = provider.GetBusinessObject<Documents>(
    BoObjectTypes.oInventoryGenExit);
goodsIssue.UserFields.Assign(udf);

var goodsReceipt = provider.GetBusinessObject<Documents>(
    BoObjectTypes.oInventoryGenEntry);
goodsReceipt.UserFields.Assign(udf);
goodsReceipt.Lines.UserFields.Assign(new MarketingDocumentRowUdf
{
    TestRow = "row value"
});
```

Null property values are ignored. This makes partial insert/update scenarios easier to express.

## UDO Provider

Get a UDO provider by registered object name:

```csharp
var udo = provider.GetUDO("ITM_SALE");
```

Read UDO metadata:

```csharp
UdoProperties properties = udo.Properties;
```

Access child tables:

```csharp
var childTable = udo.ChildTables.Item("ITM_SALE_DISC");
```

Define UDO header and child row classes:

```csharp
public class ItemSale : GeneralDataField
{
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

    public ItemSaleDiscount(
        DateTime? startDate,
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

Insert a UDO record:

```csharp
udo.Values = new ItemSale
{
    Code = "MyCode",
    ItemCode = "A0001",
    Notes = "test"
};

var discountTable = udo.ChildTables.Item("ITM_SALE_DISC");
discountTable.AddList(new List<ItemSaleDiscount>
{
    new ItemSaleDiscount(DateTime.Today, 2, 35),
    new ItemSaleDiscount(DateTime.Today.AddDays(1), 2, 30)
});

var key = udo.Insert();
```

Update a UDO record:

```csharp
udo.GetByParams<ItemSale>(new ItemSale
{
    Code = "MyCode"
});

var discountTable = udo.ChildTables.Item("ITM_SALE_DISC");
discountTable.Update(new ItemSaleDiscount(null, null, 25, lineId: 1));

udo.Update();
```

Read a UDO record:

```csharp
udo.GetByParams<ItemSale>(
    new ItemSale { Code = "MyCode" },
    SboRecordsetFillParam.FillIntoValues);

var values = (ItemSale)udo.Values;
```

## SQL Query Mapping

Manual mapping with `[SboField]`:

```csharp
public class BusinessPartner
{
    [SboField("CardCode")]
    public string Code { get; set; }

    [SboField("CardName")]
    public string Name { get; set; }
}

var sql = "SELECT CardCode, CardName FROM OCRD";
var result = provider.SqlQuery<BusinessPartner>(sql, manualColumnMapping: true);
```

Automatic mapping by property/alias name:

```csharp
public class BusinessPartner
{
    public string Code { get; set; }
    public string Name { get; set; }
}

var sql = "SELECT CardCode AS Code, CardName AS Name FROM OCRD";
var result = provider.SqlQuery<BusinessPartner>(sql);
```

## Diagnostics

The repository includes a PowerShell diagnostic tool:

```powershell
.\tools\Test-SapB1ClientLatency.ps1 `
  -Server YOUR-SAP-SERVER `
  -SapHostName YOUR-SAP-SERVER `
  -SqlServer YOUR-SAP-SERVER `
  -SqlDatabase SBO-COMMON `
  -SqlUser db-user `
  -SqlPassword "database-password" `
  -Iterations 5
```

It checks:

- DNS and hosts resolution.
- Ping and TCP latency.
- License server roundtrip.
- SLD roundtrip.
- SQL / `SBO-COMMON` roundtrip.
- SAP DI API COM load time.
- Optional operation executable startup time.
- Client route/interface to the SAP server.

See [tools/README.md](tools/README.md) for more details.

## Performance Notes

`Company.Connect()` is often affected by DNS, SLD, license server communication, SQL access to `SBO-COMMON`, antivirus scanning, and process startup overhead.

## Recommended Runtime Pattern

This library is not ideal for a request-per-request web API pattern where every HTTP request opens a new SAP DI API connection. SAP DI API is COM-based, Windows-only, and sensitive to SLD, license server, SQL, and client-machine state. A web API that creates many DI API connections concurrently can become slow or unstable.

Recommended alternatives:

- Use a small executable executor that performs one operation per run:

  ```text
  start executable -> connect -> execute one job -> disconnect -> exit
  ```

- Use an executor pool or worker pool when throughput is required:

  ```text
  queue -> limited workers -> connect -> execute job -> disconnect
  ```

- Keep the number of concurrent SAP DI API logins low. Start with one worker, then test two workers if more throughput is needed.

The built-in login gate can help protect process-per-operation executors from opening multiple DI API logins at the same time.

For process-per-operation executors:

- Keep the executable small.
- Build in Release mode.
- Use stable DNS/hosts mappings.
- Keep SAP server, license server, and SLD values explicit.
- Consider enabling the login gate to avoid multiple simultaneous DI API logins.
- Log `Connect()`, business operation, `Documents.Add()`, and `Disconnect()` separately.

If an operation such as `Documents.Add()` is slow, the bottleneck is usually inside SAP Business One posting logic rather than executor startup or DI API login. Common causes include transaction notification procedures, stock valuation, batch/serial/bin validation, database locks, and large document lines.

## Important Scope Note

This library is only a wrapper/helper around SAP Business One DI API. It does not replace SAP Business One, the DI API runtime, the license server, SLD, SQL Server, database design, stored procedures, or SAP validation logic.

If login or document processing is slow, review the whole SAP Business One environment before assuming the wrapper is the root cause. Common areas to check include:

- DNS, hosts, firewall, SLD, and license server resolution.
- SQL Server latency and access to `SBO-COMMON`.
- Database design, indexes, blocking sessions, and lock contention.
- `SBO_SP_TransactionNotification` and `SBO_SP_PostTransactionNotice`.
- Custom stored procedures, triggers, views, and add-on tables.
- SAP Business One validation rules, approvals, authorizations, and formatted searches.
- Inventory valuation, batch/serial/bin allocation, and warehouse configuration.
- The number of document lines and the amount of data posted in one transaction.
- Antivirus or endpoint protection scanning the executor, SAP DI API, or temporary files.

Use detailed timing logs around `Connect()`, data preparation, `Documents.Add()`, `GetNewObjectKey()`, post-processing, and `Disconnect()` to identify where the time is actually spent.

## Build From Source

Use Visual Studio MSBuild:

```powershell
& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" `
  .\src\SAPB1.DIAPI.Helper.csproj `
  /t:Restore,Build `
  /p:Configuration=Release
```

The project uses `COMReference` for `SAPbobsCOM`, so Visual Studio MSBuild is recommended.
