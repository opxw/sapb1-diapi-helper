param(
    [string]$Server = "YOUR-SAP-SERVER",
    [string]$SapHostName = "YOUR-SAP-SERVER",
    [string]$SqlServer = "YOUR-SAP-SERVER",
    [string]$SqlDatabase = "SBO-COMMON",
    [string]$SqlUser = "db-user",
    [string]$SqlPassword,
    [int]$SqlPort = 1433,
    [int]$LicensePort = 30000,
    [int]$SldPort = 40000,
    [int]$Iterations = 5,
    [string]$OperationExe,
    [string[]]$OperationExeArguments = @(),
    [switch]$SkipSql,
    [switch]$SkipCom,
    [switch]$TraceRoute
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Continue"

function New-Result {
    param(
        [string]$Category,
        [string]$Name,
        [object]$Value,
        [string]$Status = "OK",
        [string]$Detail = ""
    )

    [PSCustomObject]@{
        Category = $Category
        Name = $Name
        Value = $Value
        Status = $Status
        Detail = $Detail
    }
}

function Measure-ActionMs {
    param([scriptblock]$Action)

    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $result = & $Action
    $sw.Stop()

    [PSCustomObject]@{
        DurationMs = [math]::Round($sw.Elapsed.TotalMilliseconds, 2)
        Result = $result
    }
}

function Get-Stats {
    param([double[]]$Values)

    if (-not $Values -or $Values.Count -eq 0) {
        return "n/a"
    }

    $measure = $Values | Measure-Object -Minimum -Maximum -Average
    "min={0}ms avg={1}ms max={2}ms" -f `
        [math]::Round($measure.Minimum, 2), `
        [math]::Round($measure.Average, 2), `
        [math]::Round($measure.Maximum, 2)
}

function Test-DnsResolution {
    param([string]$Name)

    $ipAddress = $null
    if ([System.Net.IPAddress]::TryParse($Name, [ref]$ipAddress)) {
        return New-Result "DNS" $Name "literal IP, DNS skipped" "OK"
    }

    try {
        $measurement = Measure-ActionMs {
            Resolve-DnsName $Name -ErrorAction Stop |
                Where-Object { $_.IPAddress } |
                Select-Object -ExpandProperty IPAddress
        }

        $addresses = @($measurement.Result)
        New-Result "DNS" $Name ("{0} in {1}ms" -f (($addresses -join ", "), $measurement.DurationMs)) "OK"
    }
    catch {
        New-Result "DNS" $Name "failed" "FAIL" $_.Exception.Message
    }
}

function Test-HostsEntry {
    param([string]$Name)

    try {
        $hostsPath = "$env:windir\System32\drivers\etc\hosts"
        $matches = @(Get-Content -LiteralPath $hostsPath -ErrorAction Stop |
            Where-Object { $_ -match "^\s*[^#].*\b$([regex]::Escape($Name))\b" })

        if ($matches.Count -eq 0) {
            New-Result "Hosts" $Name "not found" "WARN"
        }
        else {
            New-Result "Hosts" $Name ($matches -join " | ") "OK"
        }
    }
    catch {
        New-Result "Hosts" $Name "failed" "FAIL" $_.Exception.Message
    }
}

function Test-PingLatency {
    param([string]$Name, [int]$Count)

    try {
        $pings = @(Test-Connection -ComputerName $Name -Count $Count -ErrorAction Stop)
        $latencies = @($pings | Select-Object -ExpandProperty ResponseTime)
        New-Result "Network" "ICMP $Name" (Get-Stats $latencies) "OK"
    }
    catch {
        New-Result "Network" "ICMP $Name" "failed" "WARN" $_.Exception.Message
    }
}

function Test-TcpLatency {
    param([string]$Name, [int]$Port, [int]$Count)

    $durations = New-Object System.Collections.Generic.List[double]
    $failures = New-Object System.Collections.Generic.List[string]

    for ($i = 0; $i -lt $Count; $i++) {
        $client = New-Object System.Net.Sockets.TcpClient
        $sw = [System.Diagnostics.Stopwatch]::StartNew()
        try {
            $async = $client.BeginConnect($Name, $Port, $null, $null)
            if (-not $async.AsyncWaitHandle.WaitOne(5000)) {
                throw "TCP connect timeout after 5000ms"
            }

            $client.EndConnect($async)
            $sw.Stop()
            $durations.Add($sw.Elapsed.TotalMilliseconds)
        }
        catch {
            $sw.Stop()
            $failures.Add($_.Exception.Message)
        }
        finally {
            $client.Close()
        }
    }

    if ($durations.Count -eq 0) {
        New-Result "Network" "TCP $Name`:$Port" "failed" "FAIL" (($failures | Select-Object -First 3) -join " | ")
    }
    else {
        $status = if ($failures.Count -eq 0) { "OK" } else { "WARN" }
        $detail = if ($failures.Count -eq 0) { "" } else { "$($failures.Count) failures: $(($failures | Select-Object -First 3) -join ' | ')" }
        New-Result "Network" "TCP $Name`:$Port" (Get-Stats $durations.ToArray()) $status $detail
    }
}

function Test-SqlRoundtrip {
    param(
        [string]$Name,
        [string]$Database,
        [string]$User,
        [string]$Password,
        [int]$Count
    )

    if ([string]::IsNullOrWhiteSpace($Password)) {
        return New-Result "SQL" "$Name/$Database" "skipped" "WARN" "SqlPassword is empty"
    }

    try {
        Add-Type -AssemblyName System.Data
    }
    catch {
        return New-Result "SQL" "$Name/$Database" "failed" "FAIL" $_.Exception.Message
    }

    $durations = New-Object System.Collections.Generic.List[double]
    $failures = New-Object System.Collections.Generic.List[string]

    for ($i = 0; $i -lt $Count; $i++) {
        $connectionString = "Server=$Name;Database=$Database;User ID=$User;Password=$Password;TrustServerCertificate=True;Connection Timeout=8;"
        $connection = New-Object System.Data.SqlClient.SqlConnection $connectionString
        $sw = [System.Diagnostics.Stopwatch]::StartNew()

        try {
            $connection.Open()
            $command = $connection.CreateCommand()
            $command.CommandText = "SELECT DB_NAME(), @@SERVERNAME"
            $null = $command.ExecuteScalar()
            $sw.Stop()
            $durations.Add($sw.Elapsed.TotalMilliseconds)
        }
        catch {
            $sw.Stop()
            $failures.Add($_.Exception.Message)
        }
        finally {
            $connection.Dispose()
        }
    }

    if ($durations.Count -eq 0) {
        New-Result "SQL" "$Name/$Database" "failed" "FAIL" (($failures | Select-Object -First 3) -join " | ")
    }
    else {
        $status = if ($failures.Count -eq 0) { "OK" } else { "WARN" }
        $detail = if ($failures.Count -eq 0) { "" } else { "$($failures.Count) failures: $(($failures | Select-Object -First 3) -join ' | ')" }
        New-Result "SQL" "$Name/$Database" (Get-Stats $durations.ToArray()) $status $detail
    }
}

function Test-ComLoad {
    param([int]$Count)

    $durations = New-Object System.Collections.Generic.List[double]
    $failures = New-Object System.Collections.Generic.List[string]
    $powerShellExe = Join-Path $PSHOME "powershell.exe"
    if (-not (Test-Path -LiteralPath $powerShellExe)) {
        $powerShellExe = "powershell.exe"
    }

    $command = @'
$ErrorActionPreference = "Stop"
$sw = [System.Diagnostics.Stopwatch]::StartNew()
$company = New-Object -ComObject SAPbobsCOM.Company
$sw.Stop()
[Console]::WriteLine([math]::Round($sw.Elapsed.TotalMilliseconds, 2))
if ($company -and [System.Runtime.InteropServices.Marshal]::IsComObject($company)) {
    [void][System.Runtime.InteropServices.Marshal]::FinalReleaseComObject($company)
}
'@
    $encodedCommand = [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($command))

    for ($i = 0; $i -lt $Count; $i++) {
        $out = [System.IO.Path]::GetTempFileName()
        $err = [System.IO.Path]::GetTempFileName()

        try {
            $process = Start-Process -FilePath $powerShellExe `
                -ArgumentList @("-NoProfile", "-ExecutionPolicy", "Bypass", "-EncodedCommand", $encodedCommand) `
                -PassThru `
                -WindowStyle Hidden `
                -RedirectStandardOutput $out `
                -RedirectStandardError $err

            if (-not $process.WaitForExit(30000)) {
                Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
                throw "COM load child process timeout after 30000ms"
            }

            $stdout = (Get-Content -LiteralPath $out -ErrorAction SilentlyContinue | Select-Object -First 1)
            $stderr = (Get-Content -LiteralPath $err -ErrorAction SilentlyContinue) -join " "
            $duration = 0.0

            if (-not [double]::TryParse($stdout, [ref]$duration)) {
                throw "child exit code $($process.ExitCode): $stderr"
            }

            $durations.Add($duration)
        }
        catch {
            $failures.Add($_.Exception.Message)
        }
        finally {
            Remove-Item -LiteralPath $out, $err -ErrorAction SilentlyContinue
        }
    }

    if ($durations.Count -eq 0) {
        New-Result "Client" "SAPbobsCOM.Company COM load isolated" "failed" "FAIL" (($failures | Select-Object -First 3) -join " | ")
    }
    else {
        $status = if ($failures.Count -eq 0) { "OK" } else { "WARN" }
        $detail = if ($failures.Count -eq 0) { "" } else { "$($failures.Count) failures: $(($failures | Select-Object -First 3) -join ' | ')" }
        New-Result "Client" "SAPbobsCOM.Company COM load isolated" (Get-Stats $durations.ToArray()) $status $detail
    }
}

function Test-ProcessStartup {
    param([string]$Exe, [string[]]$Arguments, [int]$Count)

    if ([string]::IsNullOrWhiteSpace($Exe)) {
        return New-Result "Client" "Process startup" "skipped" "WARN" "Pass -OperationExe to measure a real operation executable"
    }

    if (-not (Test-Path -LiteralPath $Exe)) {
        return New-Result "Client" "Process startup" "failed" "FAIL" "File not found: $Exe"
    }

    $durations = New-Object System.Collections.Generic.List[double]
    $failures = New-Object System.Collections.Generic.List[string]

    for ($i = 0; $i -lt $Count; $i++) {
        $sw = [System.Diagnostics.Stopwatch]::StartNew()
        try {
            $process = Start-Process -FilePath $Exe -ArgumentList $Arguments -PassThru -WindowStyle Hidden
            if (-not $process.WaitForExit(30000)) {
                Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
                throw "Process timeout after 30000ms"
            }

            $sw.Stop()
            $durations.Add($sw.Elapsed.TotalMilliseconds)
        }
        catch {
            $sw.Stop()
            $failures.Add($_.Exception.Message)
        }
    }

    if ($durations.Count -eq 0) {
        New-Result "Client" "Process startup" "failed" "FAIL" (($failures | Select-Object -First 3) -join " | ")
    }
    else {
        $status = if ($failures.Count -eq 0) { "OK" } else { "WARN" }
        $detail = if ($failures.Count -eq 0) { "" } else { "$($failures.Count) failures: $(($failures | Select-Object -First 3) -join ' | ')" }
        New-Result "Client" "Process startup $Exe" (Get-Stats $durations.ToArray()) $status $detail
    }
}

function Get-RouteInfo {
    param([string]$Name)

    try {
        $connection = Test-NetConnection $Name -Port $LicensePort -InformationLevel Detailed -WarningAction SilentlyContinue
        $source = if ($connection.SourceAddress -and $connection.SourceAddress.IPAddress) { $connection.SourceAddress.IPAddress } else { $connection.SourceAddress }
        $nextHop = if ($connection.NetRoute -and $connection.NetRoute.NextHop) { $connection.NetRoute.NextHop } else { "" }
        New-Result "Route" "Client location" "source=$source; interface=$($connection.InterfaceAlias); nextHop=$nextHop; remote=$($connection.RemoteAddress)" "OK"
    }
    catch {
        New-Result "Route" "Client location" "failed" "WARN" $_.Exception.Message
    }
}

Write-Host "SAP Business One client latency diagnostics" -ForegroundColor Cyan
Write-Host "Server=$Server SapHostName=$SapHostName SqlServer=$SqlServer SqlDatabase=$SqlDatabase Iterations=$Iterations"
Write-Host ""

$results = New-Object System.Collections.Generic.List[object]

$results.Add((Test-DnsResolution $Server))
$results.Add((Test-DnsResolution $SapHostName))
$results.Add((Test-HostsEntry $SapHostName))
$results.Add((Test-PingLatency $Server $Iterations))
$results.Add((Test-PingLatency $SapHostName $Iterations))
$results.Add((Test-TcpLatency $Server $SqlPort $Iterations))
$results.Add((Test-TcpLatency $Server $LicensePort $Iterations))
$results.Add((Test-TcpLatency $Server $SldPort $Iterations))
$results.Add((Test-TcpLatency $SapHostName $LicensePort $Iterations))
$results.Add((Test-TcpLatency $SapHostName $SldPort $Iterations))
$results.Add((Get-RouteInfo $Server))

if (-not $SkipSql) {
    $results.Add((Test-SqlRoundtrip $SqlServer $SqlDatabase $SqlUser $SqlPassword $Iterations))
}

if (-not $SkipCom) {
    $results.Add((Test-ComLoad $Iterations))
}

$results.Add((Test-ProcessStartup $OperationExe $OperationExeArguments $Iterations))

$results | Format-Table -AutoSize

if ($TraceRoute) {
    Write-Host ""
    Write-Host "Trace route to $Server" -ForegroundColor Cyan
    tracert -d $Server
}
