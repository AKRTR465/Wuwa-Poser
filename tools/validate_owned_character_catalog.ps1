[CmdletBinding()]
param([string]$Root = (Join-Path $PSScriptRoot '..'), [string]$SourcesDirectory, [string]$ReportPath)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$resolvedRoot = [IO.Path]::GetFullPath($Root)
$charactersRoot = Join-Path $resolvedRoot 'data\characters'
$catalog = Get-Content -LiteralPath (Join-Path $charactersRoot 'owned_character_catalog.json') -Raw -Encoding utf8 | ConvertFrom-Json
$certificatePath = Join-Path $charactersRoot $catalog.source_certificate_file
function Digest([string]$Path) { (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant() }
function Require([bool]$Value, [string]$Message) { if (-not $Value) { throw $Message } }
Require ((Digest $certificatePath) -ceq $catalog.source_certificate_sha256) 'Source certificate digest differs.'
$certificate = Get-Content -LiteralPath $certificatePath -Raw -Encoding utf8 | ConvertFrom-Json
$index = Get-Content -LiteralPath (Join-Path $charactersRoot 'index.json') -Raw -Encoding utf8 | ConvertFrom-Json
$nativeRoot = Join-Path $resolvedRoot 'data\native_graphs'
$nativeIndex = Get-Content -LiteralPath (Join-Path $nativeRoot 'index.json') -Raw -Encoding utf8 | ConvertFrom-Json
Require ($certificate.roles.Count -eq $catalog.counts.character_configurations -and $certificate.skins.Count -eq $catalog.characters.Count) 'Source certificate coverage differs.'
$roleIds = @($certificate.roles | ForEach-Object { Require ($_.roletype -eq 1 -and $_.istrial -eq 0) 'Non-playable role in certificate.'; $_.id } | Sort-Object -Unique)
Require ($roleIds.Count -eq $certificate.roles.Count) 'Duplicate role source ID.'
Require (@($certificate.skins | Select-Object -ExpandProperty id -Unique).Count -eq $certificate.skins.Count) 'Duplicate skin source ID.'
if ($SourcesDirectory) {
    $sourceRoot = [IO.Path]::GetFullPath($SourcesDirectory)
    foreach ($source in $certificate.sources) { Require ((Digest (Join-Path $sourceRoot $source.path)) -ceq $source.sha256) ('Raw source digest differs: '+$source.path) }
}
$results = @()
foreach ($character in $catalog.characters) {
    $skin = @($certificate.skins | Where-Object { $_.id -eq $character.skin_id })
    $association = @($certificate.associations | Where-Object { $_.skin_id -eq $character.skin_id })
    Require ($skin.Count -eq 1 -and $association.Count -eq 1 -and $skin[0].roleid -eq $character.role_id -and $roleIds -contains $character.role_id) 'Role/skin source association differs.'
    Require ([string]$skin[0].uimeshid -ceq $character.model_id -and $association[0].model_id -ceq $character.model_id -and $association[0].model_asset_name -ceq $character.model_asset_name -and $association[0].profile_id -ceq $character.profile_id) 'Model/asset source association differs.'
    $row = @($index.profiles | Where-Object { $_.profile_id -ceq $character.profile_id }); Require ($row.Count -eq 1) 'Profile source is not unique.'
    $profilePath = Join-Path $charactersRoot $row[0].profile_path
    Require ((Digest $profilePath) -ceq $row[0].profile_sha256) 'Profile digest differs.'
    $profile = Get-Content -LiteralPath $profilePath -Raw -Encoding utf8 | ConvertFrom-Json
    Require ($profile.mesh_asset_names -ccontains $character.model_asset_name -and $profile.skeleton_asset_name -ceq $association[0].skeleton_asset_name) 'Exact mesh/Skeleton profile association differs.'
    Require ($profile.bone_names.Count -eq $profile.bone_count -and $profile.bone_parents.Count -eq $profile.bone_count) 'Full reference skeleton count differs.'
    Require ((Digest (Join-Path $charactersRoot $profile.skeleton_path)) -ceq $profile.skeleton_sha256) 'Skeleton file digest differs.'
    $nativeRow = @($nativeIndex.profiles | Where-Object { $_.profile_id -ceq $character.profile_id }); Require ($nativeRow.Count -eq 1) 'Native descriptor source is not unique.'
    $descriptorPath = Join-Path $nativeRoot $nativeRow[0].runtime_path
    Require ((Digest $descriptorPath) -ceq $nativeRow[0].runtime_sha256) 'Native descriptor digest differs.'
    $descriptor = Get-Content -LiteralPath $descriptorPath -Raw -Encoding utf8 | ConvertFrom-Json
    Require ($descriptor.character_profile_sha256 -ceq (Digest $profilePath) -and $descriptor.runtime_verified -eq $false -and $descriptor.requires_live_discovery -eq $true) 'Native descriptor identity scope differs.'
    $results += [ordered]@{ character_id=$character.character_id; profile_id=$character.profile_id; mesh_asset_name=$character.model_asset_name; bone_count=$profile.bone_count; exact_mapping=$true }
}
foreach ($role in $certificate.roles) { Require (@($catalog.characters | Where-Object { $_.role_id -eq $role.id -and $_.skin_id -eq $role.skinid }).Count -eq 1) 'Default skin missing.' }
$report = [ordered]@{state='passed';scope='packaged_offline_role_skin_model_asset_certificate';role_configurations=$roleIds.Count;selections=$results.Count;missing_roles=@();missing_skins=@();mapping_mismatches=@();raw_source_hashes_checked=[bool]$SourcesDirectory;game_accessed=$false;runtime_verified=$false;results=$results}
if ($ReportPath) { $output=[IO.Path]::GetFullPath($ReportPath); [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($output)) | Out-Null; [IO.File]::WriteAllText($output,($report|ConvertTo-Json -Depth 12),[Text.UTF8Encoding]::new($false)) }
[ordered]@{state='passed';role_configurations=$roleIds.Count;selections=$results.Count;raw_source_hashes_checked=[bool]$SourcesDirectory;game_accessed=$false}|ConvertTo-Json -Compress
