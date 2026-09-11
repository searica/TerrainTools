param([string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot))

$ErrorActionPreference = "Stop"

function Assert-True([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw "VERIFY FAILED: $Message" }
}

$allSource = Get-ChildItem -LiteralPath $ProjectRoot -Recurse -Filter "*.cs" |
    Where-Object FullName -NotMatch "[\\/](bin|obj)[\\/]" |
    ForEach-Object { Get-Content -LiteralPath $_.FullName -Raw }
$source = $allSource -join "`n"
$plugin = Get-Content -LiteralPath (Join-Path $ProjectRoot "TerrainTools.cs") -Raw
$init = Get-Content -LiteralPath (Join-Path $ProjectRoot "Core\InitManager.cs") -Raw
$precise = Get-Content -LiteralPath (Join-Path $ProjectRoot "Core\PreciseTerrainModifier.cs") -Raw
$radius = Get-Content -LiteralPath (Join-Path $ProjectRoot "Core\RadiusModifier.cs") -Raw
$overlay = Get-Content -LiteralPath (Join-Path $ProjectRoot "Visualization\Overlay.cs") -Raw
$visualizers = Get-Content -LiteralPath (Join-Path $ProjectRoot "Visualization\ToolVisualizers.cs") -Raw
$shovel = Get-Content -LiteralPath (Join-Path $ProjectRoot "Core\Shovel.cs") -Raw

Assert-True ($source -notmatch 'HarmonyPatch\(typeof\(Player\),\s*nameof\(Player\.Update\)\)') "Player.Update Harmony conflict returned"
Assert-True ($plugin -match 'RadiusModifier\.Tick\(Player\.m_localPlayer\)') "radius polling is not in plugin Update"
Assert-True ($plugin -match 'SharpnessModifier\.Tick\(Player\.m_localPlayer\)') "hardness polling is not in plugin Update"
Assert-True ($precise -match 'SerializeSettingsPostfix' -and $precise -match 'DeserializeSettingsPostfix') "runtime TerrainOp settings are not serialized"
Assert-True ($precise -match 'GetRadiusPostfix' -and $precise -match 'RemoveLegacyTerrainModifiers') "cross-Heightmap restoration support is missing"
Assert-True ($init -match 'ObjectDBUpdateRegistersPostfix' -and $init -match 'm_terrainOpsByHash\.TryGetValue') "idempotent TerrainOp fallback is missing"
Assert-True ($radius -match 'RemoveModificationsOverlayVisualizer' -and $radius -match 'SetScale\(lastGhostScale\)') "restoration radius preview is incomplete"
Assert-True ($overlay -match 'psm = ps\.main' -and $overlay -match 'psm\.startSpeed\.constant') "overlay particle state is invalid"
Assert-True ($visualizers -match 'internal void SetScale') "restoration frame and marker cannot scale together"
Assert-True ($shovel -match 'UseCategories = false') "single-action shovel still uses hammer categories"

$tokens = [regex]::Matches($source, '\$atmc_[a-z0-9_]+') |
    ForEach-Object { $_.Value.TrimStart('$') } |
    Sort-Object -Unique
foreach ($language in @("English", "Russian")) {
    $path = Join-Path $ProjectRoot "Package\Translations\$language\translations.json"
    $translations = Get-Content -LiteralPath $path -Raw | ConvertFrom-Json -AsHashtable
    foreach ($token in $tokens) {
        Assert-True ($translations.ContainsKey($token)) "$language translation is missing $token"
    }
}

Write-Host "PASS: Valheim 1.0 compatibility checks"
