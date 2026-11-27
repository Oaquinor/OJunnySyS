param(
    [string]$MySqlRoot = 'C:\laragon\bin\mysql\mysql-8.4.3-winx64\bin\mysql.exe'
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$environmentFile = Join-Path $repoRoot '.env'
if (-not (Test-Path -LiteralPath $environmentFile)) { throw 'Copia .env.example a .env primero.' }
if (-not (Test-Path -LiteralPath $MySqlRoot)) { throw "No se encontró mysql.exe de Laragon: $MySqlRoot" }

$settings = @{}
foreach ($line in Get-Content -LiteralPath $environmentFile) {
    if ($line -match '^\s*([A-Za-z_][A-Za-z0-9_]*)\s*=\s*(.*?)\s*$' -and -not $line.TrimStart().StartsWith('#')) {
        $settings[$Matches[1]] = $Matches[2]
    }
}
foreach ($required in 'MYSQL_DATABASE','MYSQL_USER','MYSQL_PASSWORD') {
    if ([string]::IsNullOrWhiteSpace($settings[$required])) { throw "Define $required en .env." }
}
foreach ($name in 'MYSQL_DATABASE','MYSQL_USER','MYSQL_PASSWORD') {
    if ($settings[$name] -notmatch '^[A-Za-z0-9_!#.$-]+$') { throw "$name contiene caracteres que este script local no permite." }
}
$hostName = '127.0.0.1'
$port = if ($settings['MYSQL_PORT']) { $settings['MYSQL_PORT'] } else { '3306' }
$database = $settings['MYSQL_DATABASE']
$user = $settings['MYSQL_USER']
$password = $settings['MYSQL_PASSWORD']
$sql = "CREATE DATABASE IF NOT EXISTS ``$database`` CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci; CREATE USER IF NOT EXISTS '$user'@'$hostName' IDENTIFIED BY '$password'; ALTER USER '$user'@'$hostName' IDENTIFIED BY '$password'; GRANT ALL PRIVILEGES ON ``$database``.* TO '$user'@'$hostName';"
& $MySqlRoot --protocol=TCP --host=127.0.0.1 --port=$port --user=root --execute=$sql
if ($LASTEXITCODE -ne 0) { throw 'No se pudo preparar MySQL local. Comprueba que Laragon/MySQL esté encendido y que root no requiera contraseña.' }
Write-Host "MySQL local preparado para la base $database y el usuario $user."
