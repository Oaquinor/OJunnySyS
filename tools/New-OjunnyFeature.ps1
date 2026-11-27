param(
    [Parameter(Mandatory)]
    [ValidatePattern('^[A-Z][A-Za-z0-9]{1,39}$')]
    [string]$FeatureName,

    [Parameter(Mandatory)]
    [ValidatePattern('^[A-Za-zÁÉÍÓÚÜÑáéíóúüñ][A-Za-zÁÉÍÓÚÜÑáéíóúüñ0-9 ]{1,59}$')]
    [string]$FeatureLabel,

    [Parameter()]
    [ValidatePattern('^[a-z0-9]+(?:-[a-z0-9]+)*$')]
    [string]$Route
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$templateRoot = Join-Path $repoRoot 'templates/module'
if ([string]::IsNullOrWhiteSpace($Route)) {
    $Route = [regex]::Replace($FeatureName, '([a-z0-9])([A-Z])', '$1-$2').ToLowerInvariant()
}
$labelLower = $FeatureLabel.ToLowerInvariant()

$destinations = @(
    (Join-Path $repoRoot "src/OJunnySyS.Domain/Features/$FeatureName"),
    (Join-Path $repoRoot "src/OJunnySyS.Application/Features/$FeatureName"),
    (Join-Path $repoRoot "src/OJunnySyS.Web/Components/Features/$FeatureName")
)
foreach ($destination in $destinations) {
    if (Test-Path -LiteralPath $destination) {
        throw "El módulo ya existe: $destination"
    }
}

$replacements = @{
    '__FEATURE_NAME__' = $FeatureName
    '__FEATURE_LABEL__' = $FeatureLabel
    '__FEATURE_LABEL_LOWER__' = $labelLower
    '__FEATURE_UPPER__' = $FeatureName.ToUpperInvariant()
    '__FEATURE_ROUTE__' = $Route
}

$files = Get-ChildItem -LiteralPath $templateRoot -Recurse -File -Filter '*.template'
foreach ($file in $files) {
    $relativePath = $file.FullName.Substring($templateRoot.Length).TrimStart([char[]]@('\', '/'))
    $relativeParts = $relativePath -split '[\\/]'
    $area = $relativeParts[0]
    $destinationRoot = switch ($area) {
        'Domain' { $destinations[0] }
        'Application' { $destinations[1] }
        'Web' { $destinations[2] }
        default { throw "Ubicación de plantilla desconocida: $relativePath" }
    }
    $fileName = [System.IO.Path]::GetFileNameWithoutExtension($file.Name)
    $outputName = $fileName.Replace('Feature', $FeatureName)
    $outputPath = Join-Path $destinationRoot $outputName
    $content = Get-Content -LiteralPath $file.FullName -Raw
    foreach ($token in $replacements.Keys) {
        $content = $content.Replace($token, $replacements[$token])
    }
    New-Item -ItemType Directory -Path $destinationRoot -Force | Out-Null
    Set-Content -LiteralPath $outputPath -Value $content -Encoding utf8
}

$notesSource = Join-Path $templateRoot 'README.md.template'
$notesPath = Join-Path (Join-Path $repoRoot 'docs/modules') "$FeatureName.md"
$notes = Get-Content -LiteralPath $notesSource -Raw
foreach ($token in $replacements.Keys) {
    $notes = $notes.Replace($token, $replacements[$token])
}
New-Item -ItemType Directory -Path (Split-Path $notesPath) -Force | Out-Null
Set-Content -LiteralPath $notesPath -Value $notes -Encoding utf8

Write-Host "Módulo $FeatureLabel creado. Completa persistencia y registro de dependencias antes de compilar."
