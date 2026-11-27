param([switch]$NoLaunchProfile, [ValidateSet('Debug','Release','Review')][string]$Configuration = 'Debug', [ValidateRange(1,65535)][int]$Port = 5144)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$environmentFile = Join-Path $repoRoot '.env'
if (-not (Test-Path -LiteralPath $environmentFile)) {
    throw 'Falta .env. Copia .env.example a .env y define la contraseña del administrador local antes de iniciar.'
}

$settings = @{}
foreach ($line in Get-Content -LiteralPath $environmentFile) {
    if ($line -match '^\s*([A-Za-z_][A-Za-z0-9_]*)\s*=\s*(.*?)\s*$' -and -not $line.TrimStart().StartsWith('#')) {
        $settings[$Matches[1]] = $Matches[2]
    }
}
foreach ($required in 'MYSQL_DATABASE','MYSQL_USER','MYSQL_PASSWORD','BOOTSTRAP_ADMIN_EMAIL','BOOTSTRAP_ADMIN_PASSWORD') {
    if ([string]::IsNullOrWhiteSpace($settings[$required])) { throw "Define $required en .env." }
}

$mysqlPort = if ($settings['MYSQL_PORT']) { $settings['MYSQL_PORT'] } else { '3306' }
$env:ConnectionStrings__OjunnySyS = "Server=127.0.0.1;Port=$mysqlPort;Database=$($settings['MYSQL_DATABASE']);User ID=$($settings['MYSQL_USER']);Password=$($settings['MYSQL_PASSWORD']);"
$env:BootstrapAdmin__Email = $settings['BOOTSTRAP_ADMIN_EMAIL']
$env:BootstrapAdmin__Password = $settings['BOOTSTRAP_ADMIN_PASSWORD']
$env:BootstrapAdmin__DisplayName = if ($settings['BOOTSTRAP_ADMIN_DISPLAY_NAME']) { $settings['BOOTSTRAP_ADMIN_DISPLAY_NAME'] } else { 'Administrador' }
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:ASPNETCORE_URLS = "http://localhost:$Port"

Push-Location $repoRoot
try {
    if ($NoLaunchProfile) { dotnet run --project src/OJunnySyS.Web --configuration $Configuration --no-launch-profile --urls "http://localhost:$Port" }
    else { dotnet run --project src/OJunnySyS.Web --configuration $Configuration --launch-profile http }
    if ($LASTEXITCODE -ne 0) { throw "La aplicación terminó con código $LASTEXITCODE." }
}
finally { Pop-Location }
