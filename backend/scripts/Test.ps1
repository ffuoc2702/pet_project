[CmdletBinding()]
param(
    [string] $DatabaseContainer = 'matchingsports-postgres-dev',
    [switch] $NoBuild
)

$ErrorActionPreference = 'Stop'
$previousConnection = $env:MATCHINGSPORTS_RBAC_TEST_CONNECTION
Push-Location (Split-Path $PSScriptRoot -Parent)
try {
    if ([string]::IsNullOrWhiteSpace($previousConnection)) {
        $containerJson = docker inspect $DatabaseContainer
        if ($LASTEXITCODE -ne 0) {
            throw 'Không truy cập được container PostgreSQL. Hãy bật Docker hoặc đặt MATCHINGSPORTS_RBAC_TEST_CONNECTION.'
        }
        $container = ($containerJson | ConvertFrom-Json)[0]
        if (-not $container.State.Running) {
            throw 'Container PostgreSQL chưa chạy.'
        }
        $postgresVariables = @{}
        foreach ($entry in $container.Config.Env) {
            $parts = $entry.Split('=', 2)
            $postgresVariables[$parts[0]] = $parts[1]
        }
        $publishedPort = @($container.NetworkSettings.Ports.'5432/tcp')[0]
        if ($null -eq $publishedPort) {
            throw 'Container chưa publish cổng PostgreSQL 5432 ra máy host.'
        }
        $connection = [System.Data.Common.DbConnectionStringBuilder]::new()
        $connection['Host'] = '127.0.0.1'
        $connection['Port'] = $publishedPort.HostPort
        $connection['Database'] = 'matchingsports_rbac_test_local'
        $connection['Username'] = if ($postgresVariables['POSTGRES_USER']) { $postgresVariables['POSTGRES_USER'] } else { 'postgres' }
        $connection['Password'] = $postgresVariables['POSTGRES_PASSWORD']
        if ([string]::IsNullOrWhiteSpace($connection['Password'])) {
            throw 'Không tìm thấy POSTGRES_PASSWORD. Hãy đặt MATCHINGSPORTS_RBAC_TEST_CONNECTION trong môi trường chạy test.'
        }
        $env:MATCHINGSPORTS_RBAC_TEST_CONNECTION = $connection.ConnectionString
        Write-Host 'Đã cấu hình kết nối test từ container PostgreSQL; mỗi test tự tạo và dọn database riêng.'
    }

    $testArguments = @('test', './MatchingSports.slnx', '--configuration', 'Release', '--no-restore')
    if ($NoBuild) { $testArguments += '--no-build' }
    & dotnet @testArguments
    if ($LASTEXITCODE -ne 0) {
        throw "Bộ test thất bại (exit code $LASTEXITCODE)."
    }
}
finally {
    $env:MATCHINGSPORTS_RBAC_TEST_CONNECTION = $previousConnection
    Pop-Location
}
