[CmdletBinding()]
param([string]$Root = (Join-Path $PSScriptRoot '..'), [switch]$CheckSources, [string]$ReportPath)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$resolvedRoot = [IO.Path]::GetFullPath($Root)
function ReadJson([string]$Relative) { Get-Content -LiteralPath (Join-Path $resolvedRoot $Relative) -Raw -Encoding utf8 | ConvertFrom-Json }
function Digest([string]$Relative) { (Get-FileHash -LiteralPath (Join-Path $resolvedRoot $Relative) -Algorithm SHA256).Hash.ToLowerInvariant() }
function Require([bool]$Value, [string]$Message) { if (-not $Value) { throw $Message } }
$catalog = ReadJson 'data/characters/owned_character_catalog.json'
$forms = ReadJson 'data/characters/character_forms.json'
$index = ReadJson 'data/characters/index.json'
$native = ReadJson 'data/native_graphs/index.json'
Require ($forms.schema -eq 1 -and $forms.runtime_verified -eq $false -and $forms.game_accessed -eq $false) 'Form certificate scope differs.'
Require ($forms.forms.Count -eq $forms.counts.additional_forms -and $catalog.characters.Count + $forms.forms.Count -eq $forms.counts.combined_selections) 'Form coverage differs.'
$ids = @($catalog.characters.character_id) + @($forms.forms.character_id)
Require (@($ids | Select-Object -Unique).Count -eq $ids.Count) 'Duplicate form selection ID.'
if ($CheckSources) { foreach ($source in $forms.sources) { Require ((Digest $source.path) -ceq $source.sha256) ('Form raw source digest differs: '+$source.path) } }
$results = @()
foreach ($form in $forms.forms) {
    $base = @($catalog.characters | Where-Object character_id -CEQ $form.base_character_id)
    Require ($base.Count -eq 1) 'Form base selection is missing.'
    $load = $form.load
    Require ($load.id -match '^[a-z0-9_-]+$' -and $load.strategy -in @('role_morph','mesh_parts') -and $load.base_model_id -ceq $base[0].model_id) 'Form load contract differs.'
    $profile = @($index.profiles | Where-Object profile_id -CEQ $form.profile_id)
    Require ($profile.Count -eq 1 -and $profile[0].mesh_asset_names -ccontains $form.model_asset_name) 'Form mesh profile is missing.'
    $profilePath = 'data/characters/'+$profile[0].profile_path
    Require ((Digest $profilePath) -ceq $profile[0].profile_sha256) 'Form profile digest differs.'
    $character = ReadJson $profilePath
    Require ((Digest ('data/characters/'+$character.skeleton_path)) -ceq $character.skeleton_sha256) 'Form skeleton digest differs.'
    Require ($character.bone_names.Count -eq $character.bone_count -and $character.bone_parents.Count -eq $character.bone_count) 'Form skeleton is incomplete.'
    $row = @($native.profiles | Where-Object profile_id -CEQ $form.profile_id)
    Require ($row.Count -eq 1) 'Form native descriptor is missing.'
    $descriptorPath = 'data/native_graphs/'+$row[0].runtime_path
    Require ((Digest $descriptorPath) -ceq $row[0].runtime_sha256) 'Form native descriptor digest differs.'
    $descriptor = ReadJson $descriptorPath
    Require ($descriptor.character_profile_sha256 -ceq (Digest $profilePath) -and $descriptor.skeleton_sha256 -ceq $character.skeleton_sha256 -and $descriptor.requires_live_discovery -eq $true -and $descriptor.runtime_verified -eq $false) 'Form native identity scope differs.'
    $graphs = @($descriptor.candidate_sources | Where-Object { $_.kind -eq 'ui_export' -and $_.ui_class_asset -ceq $load.anim_class_path -and $_.evidence_kind -eq 'actual_nodes' })
    Require ($graphs.Count -eq 1 -and $graphs[0].group_signature.kawaii_group_count -gt 0) 'Exact form UI physics source is missing.'
    Require ($load.main_mesh_path.Split('.')[-1] -ceq $form.model_asset_name) 'Form primary mesh differs.'
    if ($load.strategy -eq 'role_morph') {
        $source = $form.evidence.row
        Require ($source.roleid -eq $base[0].role_id -and $source.skinid -eq $base[0].skin_id -and $source.morph -eq $load.morph_type -and [string]$source.uimeshid -ceq $form.model_id -and $source.uisceneperformanceabp -ceq $load.anim_class_path) 'RoleMorph source association differs.'
    } else {
        Require ($form.evidence.kind -eq 'sequence_mesh_composition' -and $load.child_mesh_paths.Count -eq 1 -and $character.morph_count -gt 0) 'Split form composition is incomplete.'
    }
    $results += [ordered]@{ character_id=$form.character_id;profile_id=$form.profile_id;bone_count=$character.bone_count;morph_count=$character.morph_count;native_groups=$graphs[0].group_signature.kawaii_group_count;runtime_verified=$false }
}
$report = [ordered]@{state='passed';scope='offline_character_form_sources_and_profiles';additional_forms=$results.Count;combined_selections=$ids.Count;raw_sources_checked=[bool]$CheckSources;game_accessed=$false;runtime_verified=$false;results=$results}
if ($ReportPath) { $path = [IO.Path]::GetFullPath($ReportPath); [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($path)) | Out-Null; [IO.File]::WriteAllText($path,($report|ConvertTo-Json -Depth 10),[Text.UTF8Encoding]::new($false)) }
$report | ConvertTo-Json -Depth 8
