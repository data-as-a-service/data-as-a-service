param(
    [Parameter(Mandatory = $true)]
    [string]$Server
)

$ErrorActionPreference = 'Stop'

$sqlcmdCommand = Get-Command sqlcmd -ErrorAction SilentlyContinue
if ($null -eq $sqlcmdCommand) {
    throw 'sqlcmd is required. Install the SQL Server command-line tools, then run this script again.'
}

$initializeScript = Join-Path $PSScriptRoot 'initialize-database.sql'
$migrationDirectory = Join-Path $PSScriptRoot 'database-updates'

function Invoke-SqlScript([string]$ScriptPath, [string[]]$SqlcmdArguments) {
    Write-Host "Applying $([System.IO.Path]::GetFileName($ScriptPath))…"
    & $sqlcmdCommand.Source @SqlcmdArguments -b -i $ScriptPath
    if ($LASTEXITCODE -ne 0) {
        throw "sqlcmd failed while applying '$ScriptPath' (exit code $LASTEXITCODE)."
    }
}

Invoke-SqlScript $initializeScript @('-S', $Server, '-E')

$migrationFiles = Get-ChildItem -LiteralPath $migrationDirectory -Filter '*.sql' -File |
    Sort-Object -Property Name

foreach ($migrationFile in $migrationFiles) {
    Invoke-SqlScript $migrationFile.FullName @('-S', $Server, '-E', '-d', 'Daas')
}

Write-Host 'Database setup and updates are complete.'
