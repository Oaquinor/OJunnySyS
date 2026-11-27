$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$environmentFile = Join-Path $repoRoot '.env'
if (-not (Test-Path -LiteralPath $environmentFile)) { throw 'Falta .env con la conexión local de Laragon.' }

$settings = @{}
foreach ($line in Get-Content -LiteralPath $environmentFile) {
    if ($line -match '^\s*([A-Za-z_][A-Za-z0-9_]*)\s*=\s*(.*?)\s*$' -and -not $line.TrimStart().StartsWith('#')) {
        $settings[$Matches[1]] = $Matches[2]
    }
}
foreach ($required in 'MYSQL_USER','MYSQL_PASSWORD') {
    if ([string]::IsNullOrWhiteSpace($settings[$required])) { throw "Define $required en .env." }
}

$mysqlExecutable = (Get-Command mysql.exe -ErrorAction SilentlyContinue).Source
if (-not $mysqlExecutable) {
    $mysqlExecutable = Get-ChildItem -LiteralPath 'C:\laragon\bin\mysql' -Directory -ErrorAction SilentlyContinue |
        Sort-Object Name -Descending |
        ForEach-Object { Join-Path $_.FullName 'bin\mysql.exe' } |
        Where-Object { Test-Path -LiteralPath $_ } |
        Select-Object -First 1
}
if (-not $mysqlExecutable) { throw 'No encontré mysql.exe de Laragon.' }

$mysqlPort = if ($settings['MYSQL_PORT']) { $settings['MYSQL_PORT'] } else { '3306' }
$testDatabase = 'ojunnysy_test_' + [Guid]::NewGuid().ToString('N').Substring(0, 12)
if ($testDatabase -notmatch '^ojunnysy_test_[a-f0-9]{12}$') { throw 'El nombre de la base temporal no superó la validación.' }
$previousMysqlPassword = $env:MYSQL_PWD
$previousTestConnection = $env:OJUNNYSYS_TEST_CONNECTION
$databaseCreated = $false

try {
    # The application user is intentionally not granted CREATE/DROP DATABASE.
    # Laragon's local root is used only for this regex-validated disposable database.
    $env:MYSQL_PWD = $null
    $createSql = "CREATE DATABASE ``$testDatabase`` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;"
    & $mysqlExecutable --host=127.0.0.1 "--port=$mysqlPort" --user=root --default-character-set=utf8mb4 "--execute=$createSql"
    if ($LASTEXITCODE -ne 0) { throw 'No se pudo crear la base MySQL temporal. Verifica los permisos de Laragon; la base principal no fue modificada.' }
    $databaseCreated = $true
    $escapedApplicationUser = $settings['MYSQL_USER'].Replace("'", "''")
    $grantSql = "GRANT ALL PRIVILEGES ON ``$testDatabase``.* TO '$escapedApplicationUser'@'127.0.0.1';"
    & $mysqlExecutable --host=127.0.0.1 "--port=$mysqlPort" --user=root --default-character-set=utf8mb4 "--execute=$grantSql"
    if ($LASTEXITCODE -ne 0) { throw 'No se pudieron asignar permisos temporales a la base de pruebas.' }

    $env:OJUNNYSYS_TEST_CONNECTION = "Server=127.0.0.1;Port=$mysqlPort;Database=$testDatabase;User ID=$($settings['MYSQL_USER']);Password=$($settings['MYSQL_PASSWORD']);"
    Push-Location $repoRoot
    try {
        dotnet test tests/OJunnySyS.Infrastructure.IntegrationTests/OJunnySyS.Infrastructure.IntegrationTests.csproj --configuration BankModel
        if ($LASTEXITCODE -ne 0) { throw 'Falló la prueba de integración MySQL de conciliación bancaria.' }
    }
    finally { Pop-Location }
}
finally {
    if ($databaseCreated -and $testDatabase -match '^ojunnysy_test_[a-f0-9]{12}$') {
        $escapedApplicationUser = $settings['MYSQL_USER'].Replace("'", "''")
        $dropSql = "REVOKE ALL PRIVILEGES ON ``$testDatabase``.* FROM '$escapedApplicationUser'@'127.0.0.1'; DROP DATABASE IF EXISTS ``$testDatabase``;"
        $env:MYSQL_PWD = $null
        & $mysqlExecutable --host=127.0.0.1 "--port=$mysqlPort" --user=root --default-character-set=utf8mb4 "--execute=$dropSql"
        if ($LASTEXITCODE -ne 0) { Write-Warning 'No se pudo eliminar la base temporal; revisa las bases con prefijo ojunnysy_test_.' }
    }
    $env:MYSQL_PWD = $previousMysqlPassword
    $env:OJUNNYSYS_TEST_CONNECTION = $previousTestConnection
}
