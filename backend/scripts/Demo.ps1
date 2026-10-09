[CmdletBinding()]
param(
    [int] $Users = 50,
    [int] $Teams = 6,
    [int] $Listings = 15,
    [string] $RunId = 'cv-demo-01',
    [ValidateSet('demo', 'test')] [string] $Mode = 'demo',
    [string] $DatabaseContainer = 'matchingsports-postgres-dev',
    [switch] $UseUserSecrets,
    [switch] $NoBuild
)
$ErrorActionPreference = 'Stop'
$previousConnection = $env:ConnectionStrings__matchingsports
$previousPassword = $env:Demo__Password
$securePassword = $null
Push-Location (Split-Path $PSScriptRoot -Parent)
try {
    if (-not $UseUserSecrets) {
        if ([string]::IsNullOrWhiteSpace($previousConnection)) {
            $containerJson = docker inspect $DatabaseContainer
            if ($LASTEXITCODE -ne 0) { throw 'Không truy cập được container PostgreSQL.' }
            $container = ($containerJson | ConvertFrom-Json)[0]
            if (-not $container.State.Running) { throw 'Container PostgreSQL chưa chạy.' }
            $variables = @{}
            foreach ($entry in $container.Config.Env) {
                $parts = $entry.Split('=', 2)
                $variables[$parts[0]] = $parts[1]
            }
            $port = @($container.NetworkSettings.Ports.'5432/tcp')[0]
            if ($null -eq $port) { throw 'Container chưa publish cổng PostgreSQL.' }
            $connection = [System.Data.Common.DbConnectionStringBuilder]::new()
            $connection['Host'] = '127.0.0.1'
            $connection['Port'] = $port.HostPort
            $connection['Database'] = if ($variables['POSTGRES_DB']) { $variables['POSTGRES_DB'] } else { 'postgres' }
            $connection['Username'] = if ($variables['POSTGRES_USER']) { $variables['POSTGRES_USER'] } else { 'postgres' }
            $connection['Password'] = $variables['POSTGRES_PASSWORD']
            if ([string]::IsNullOrWhiteSpace($connection['Password'])) { throw 'Cần ConnectionStrings__matchingsports.' }
            $env:ConnectionStrings__matchingsports = $connection.ConnectionString
        }
        if ([string]::IsNullOrWhiteSpace($previousPassword)) {
            $securePassword = Read-Host 'Mật khẩu chung cho tài khoản demo (ít nhất 10 ký tự, hoa/thường/số/ký tự đặc biệt)' -AsSecureString
            $env:Demo__Password = [System.Net.NetworkCredential]::new('', $securePassword).Password
        }
    }
    $arguments = @('run', '--project', 'tools/MatchingSports.DemoSimulator', '--configuration', 'Release')
    if ($NoBuild) { $arguments += '--no-build' }
    $arguments += @('--', '--users', $Users, '--teams', $Teams, '--listings', $Listings, '--run-id', $RunId, '--mode', $Mode)
    & dotnet @arguments
    if ($LASTEXITCODE -ne 0) { throw "Demo thất bại (exit code $LASTEXITCODE); kiểm tra checkpoint rồi chạy lại cùng RunId." }
}
finally {
    $env:ConnectionStrings__matchingsports = $previousConnection
    $env:Demo__Password = $previousPassword
    if ($null -ne $securePassword) { $securePassword.Dispose() }
    Pop-Location
}
