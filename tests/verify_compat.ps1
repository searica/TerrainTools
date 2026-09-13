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
$player = Get-Content -LiteralPath (Join-Path $ProjectRoot "Patches\PlayerPatch.cs") -Raw
$camera = Get-Content -LiteralPath (Join-Path $ProjectRoot "Patches\GameCameraPatch.cs") -Raw
$overlayVisualizer = Get-Content -LiteralPath (Join-Path $ProjectRoot "Visualization\OverlayVisualizer.cs") -Raw
$project = Get-Content -LiteralPath (Join-Path $ProjectRoot "TerrainTools.csproj") -Raw
$assemblyInfo = Get-Content -LiteralPath (Join-Path $ProjectRoot "Properties\AssemblyInfo.cs") -Raw
$overlay = Get-Content -LiteralPath (Join-Path $ProjectRoot "Visualization\Overlay.cs") -Raw
$visualizers = Get-Content -LiteralPath (Join-Path $ProjectRoot "Visualization\ToolVisualizers.cs") -Raw
$shovel = Get-Content -LiteralPath (Join-Path $ProjectRoot "Core\Shovel.cs") -Raw
$iconCache = Get-Content -LiteralPath (Join-Path $ProjectRoot "Visualization\IconCache.cs") -Raw
$packageTargets = Get-Content -LiteralPath (Join-Path $ProjectRoot "ModPackageTool.targets") -Raw
$environment = Get-Content -LiteralPath (Join-Path $ProjectRoot "environment.props") -Raw

Assert-True ($source -notmatch 'HarmonyPatch\(typeof\(Player\),\s*nameof\(Player\.Update\)\)') "Player.Update Harmony conflict returned"
Assert-True ($plugin -match 'RadiusModifier\.Tick\(Player\.m_localPlayer\)') "radius polling is not in plugin Update"
Assert-True ($plugin -match 'SharpnessModifier\.Tick\(Player\.m_localPlayer\)') "hardness polling is not in plugin Update"
Assert-True ($precise -match 'SerializeSettingsPostfix' -and $precise -match 'DeserializeSettingsPostfix') "runtime TerrainOp settings are not serialized"
Assert-True ($precise -match 'class RuntimeSettings' -and $precise -match 'modifier is RuntimeSettings \{ IsReset: true \}') "foreign empty terrain operations can trigger reset"
Assert-True ($precise -match '__instance is not RuntimeSettings settings' -and $precise -match 'InitManager\.IsCustomTool') "runtime settings are not scoped to managed operations"
Assert-True ($precise -match 'pkg\.Size\(\) - payloadStart < payloadSize[\s\S]*?__result = null') "recognized truncated settings are not rejected"
Assert-True ($precise -match 'float\.IsNaN' -and $precise -match 'float\.IsInfinity' -and $precise -match 'if \(!IsValid\(settings\)\)[\s\S]*?__result = null') "invalid network settings are not rejected"
Assert-True ($precise -match 'PrivateArea\.CheckAccess\(position, radius, flash, true\)' -and $precise -match 'radius \*= 1\.414214f') "full brush footprint is not ward-checked"
Assert-True ($precise -match 'm_playerModifiction' -and $precise -match 'modifier\.m_nview\.IsValid\(\)') "legacy reset is not limited to valid player modifiers"
Assert-True ($precise -match 'Mathf\.Abs\(modifier\.transform\.position\.x - position\.x\) \+ modifierRadius > radius' -and $precise -match 'Mathf\.Abs\(modifier\.transform\.position\.z - position\.z\) \+ modifierRadius > radius') "legacy reset can remove modifiers outside the selected square"
Assert-True ($precise -match 'GetRadiusPostfix' -and $precise -match 'RemoveLegacyTerrainModifiers') "cross-Heightmap restoration support is missing"
Assert-True ($precise -match 'GetComponentInParent<Piece>' -and $precise -match 'GetComponentInParent<WearNTear>') "structure protection is missing"
Assert-True ($precise -notmatch '\[HarmonyPatch\(typeof\(PreciseTerrainModifier\)\)\]') "redundant class-level Harmony target returned"
Assert-True ($init -match 'ObjectDBUpdateRegistersPostfix' -and $init -match 'm_terrainOpsByHash\.TryGetValue') "idempotent TerrainOp fallback is missing"
Assert-True ($radius -match 'RemoveModificationsOverlayVisualizer' -and $radius -match 'SetScale\(lastGhostScale\)') "restoration radius preview is incomplete"
Assert-True ($radius -match 'activeRadiusTool' -and $radius -match 'SelectRadiusTool' -and $radius -match 'IsActiveToolInstance') "radius state can leak across tools"
Assert-True ($radius -match 'TerrainCompExtensions\.FixedRadius' -and $radius -match 'IsActiveToolInstance\(ghostTerrainOp\)' -and $radius -match 'ghostTerrainOp\.m_settings\.m_levelRadius = lastModdedRadius') "reset radius minimum, active-tool guard, or ghost settings are not preserved"
Assert-True ($player -match '\[HarmonyPostfix\]' -and $player -notmatch '\[HarmonyFinalizer\]' -and $player -match 'overlay\.Refresh\(\);\s*RadiusModifier\.RefreshGhostScale') "placement preview refresh ordering can jitter"
Assert-True ($overlayVisualizer -match 'internal void Refresh\(\)' -and $overlayVisualizer -notmatch 'private void Update\(\)') "overlay still races the placement ghost update"
Assert-True ($camera -match 'matches\.Count != 1' -and $camera -match 'replacement\.labels\.AddRange' -and $camera -match 'replacement\.blocks\.AddRange') "camera transpiler does not fail safely"
Assert-True ($overlay -match 'psm = ps\.main' -and $overlay -match 'psm\.startSpeed\.constant') "overlay particle state is invalid"
Assert-True ($visualizers -match 'internal void SetScale') "restoration frame and marker cannot scale together"
Assert-True ($shovel -match 'UseCategories = false') "single-action shovel still uses hammer categories"
Assert-True ($iconCache -match 'AppDomain\.CurrentDomain\.GetAssemblies') "ImageConversion assembly fallback is missing"
Assert-True ($packageTargets -match 'OutputResources\)\\Translations') "debug translations are not deployed"
Assert-True ($environment -notmatch 'VALHEIM_SERVERR') "dedicated-server property typo returned"

$tokens = [regex]::Matches($source, '\$atmc_[a-z0-9_]+') |
    ForEach-Object { $_.Value.TrimStart('$') } |
    Sort-Object -Unique
foreach ($language in @("English", "Russian")) {
    $path = Join-Path $ProjectRoot "Package\Translations\TerrainTools\$language\translations.json"
    Assert-True (-not (Test-Path -LiteralPath (Join-Path $ProjectRoot "Package\Translations\$language\translations.json"))) "$language translation still uses the ambiguous legacy path"
    $translations = Get-Content -LiteralPath $path -Raw | ConvertFrom-Json -AsHashtable
    foreach ($token in $tokens) {
        Assert-True ($translations.ContainsKey($token)) "$language translation is missing $token"
    }
}

Assert-True ($plugin -match 'Path\.GetDirectoryName\(Info\.Location\)' -and $plugin -match '"Translations",\s*"TerrainTools",\s*language' -and $plugin -match 'AddFileByPath\(externalPath, true\)') "external localization path or precedence is not explicit"
Assert-True ($project -match 'Package\\Translations\\TerrainTools\\English\\translations\.json' -and $project -match 'Package\\Translations\\TerrainTools\\Russian\\translations\.json') "namespaced translations are not embedded"
Assert-True ($assemblyInfo -match 'AssemblyVersion\("1\.4\.4"\)' -and $assemblyInfo -match 'AssemblyFileVersion\("1\.4\.4"\)') "assembly and plugin versions do not match"

Write-Host "PASS: Valheim 1.0 compatibility checks"
