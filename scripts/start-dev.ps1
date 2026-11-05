$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$apiProject = Join-Path $repoRoot 'src\servers\Web\Daas.Api\Daas.Api.csproj'
$frontendRoot = Join-Path $repoRoot 'src\frontend\daas-client'
$apiUrl = 'http://localhost:5247'
$swaggerUrl = "$apiUrl/swagger/v1/swagger.json"

function Stop-ProcessTree([System.Diagnostics.Process]$Process) {
    if ($null -ne $Process -and -not $Process.HasExited) {
        & taskkill.exe /PID $Process.Id /T /F *> $null
    }
}

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw 'The .NET SDK is required. Install it, then run this script again.'
}
if (-not (Get-Command npm.cmd -ErrorAction SilentlyContinue)) {
    throw 'Node.js and npm are required. Install them, then run this script again.'
}

if (-not (Test-Path (Join-Path $frontendRoot 'node_modules'))) {
    Write-Host 'Installing frontend dependencies…'
    Push-Location $frontendRoot
    try {
        & npm.cmd ci
        if ($LASTEXITCODE -ne 0) { throw 'Frontend dependency installation failed.' }
    }
    finally { Pop-Location }
}

$apiProcess = $null
$frontendProcess = $null
try {
    Write-Host 'Starting the API…'
    $apiArguments = "run --project `"$apiProject`" --launch-profile http"
    $apiProcess = Start-Process -FilePath 'dotnet' -ArgumentList $apiArguments -WorkingDirectory $repoRoot -PassThru -NoNewWindow

    $deadline = (Get-Date).AddSeconds(90)
    $apiReady = $false
    while ((Get-Date) -lt $deadline) {
        if ($apiProcess.HasExited) { throw "The API stopped before becoming ready (exit code $($apiProcess.ExitCode))." }
        try {
            $null = Invoke-WebRequest -Uri $swaggerUrl -TimeoutSec 2 -UseBasicParsing
            $apiReady = $true
            break
        }
        catch { Start-Sleep -Seconds 1 }
    }
    if (-not $apiReady) { throw "The API did not become ready within 90 seconds. Check the API output and database configuration." }

    Write-Host "API is ready at $apiUrl. Starting the React frontend…"
    $frontendProcess = Start-Process -FilePath 'cmd.exe' -ArgumentList '/c npm.cmd run dev -- --host 127.0.0.1 --strictPort' -WorkingDirectory $frontendRoot -PassThru -NoNewWindow
    Write-Host 'Frontend: http://127.0.0.1:5173'
    Write-Host 'Press Ctrl+C to stop both services.'

    while ($true) {
        if ($apiProcess.HasExited) { throw "The API exited with code $($apiProcess.ExitCode)." }
        if ($frontendProcess.HasExited) { throw "The frontend exited with code $($frontendProcess.ExitCode)." }
        Start-Sleep -Seconds 1
    }
}
finally {
    Stop-ProcessTree $frontendProcess
    Stop-ProcessTree $apiProcess
}
