# SAP Business One Client Diagnostics

`Test-SapB1ClientLatency.ps1` checks the common causes of slow SAP Business One DI API login from a client machine:

- network latency
- DNS and hosts resolution
- license server TCP roundtrip
- SLD TCP roundtrip
- SQL / `SBO-COMMON` roundtrip
- SAP DI API COM load time
- optional operation executable startup time
- client route/interface to the SAP server

## Example

```powershell
cd D:\projects\git\sapb1-diapi-helper

.\tools\Test-SapB1ClientLatency.ps1 `
  -Server YOUR-SAP-SERVER `
  -SapHostName YOUR-SAP-SERVER `
  -SqlServer YOUR-SAP-SERVER `
  -SqlDatabase SBO-COMMON `
  -SqlUser db-user `
  -SqlPassword "database-password" `
  -Iterations 5
```

## With Operation EXE Startup Measurement

```powershell
.\tools\Test-SapB1ClientLatency.ps1 `
  -SqlPassword "database-password" `
  -OperationExe "C:\Path\To\YourAddonOperation.exe" `
  -OperationExeArguments @("--help")
```

Use an argument that starts and exits quickly. This check is meant to estimate process startup / antivirus scanning overhead, not to run a full SAP operation.

## Optional Trace Route

```powershell
.\tools\Test-SapB1ClientLatency.ps1 -SqlPassword "database-password" -TraceRoute
```
