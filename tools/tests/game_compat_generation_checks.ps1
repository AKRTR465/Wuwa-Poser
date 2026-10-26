[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$generator=Join-Path $repo 'tools/generate_game_compat.ps1'
$original=Join-Path $repo 'data/game_compat/ww_runtime.json'
$fixture=[IO.Path]::GetFullPath((Join-Path ([IO.Path]::GetTempPath()) ('wwposer-compat-'+[Guid]::NewGuid().ToString('N'))))
$source=Join-Path $fixture 'source.json'
$script:compatChecks=0
function Save-Profile($Profile){[IO.File]::WriteAllText($source,($Profile|ConvertTo-Json -Depth 16)+"`n",[Text.UTF8Encoding]::new($false))}
function Reject([scriptblock]$Action,[string]$Reason){
 $rejected=$false
 try{& $Action | Out-Null}catch{$rejected=$true}
 if(-not $rejected){throw ('Compatibility generator accepted '+$Reason)}
 $script:compatChecks++
}
try{
 [IO.Directory]::CreateDirectory($fixture)|Out-Null
 Copy-Item -LiteralPath $original -Destination $source
 & $generator -SourcePath $source -OutputRoot $fixture | Out-Null
 & $generator -SourcePath $source -OutputRoot $fixture -Check | Out-Null
 $script:compatChecks++
 $header=Join-Path $fixture 'runtime/generated/GameCompat.generated.h'
 $before=[IO.File]::ReadAllText($header)
 & $generator -SourcePath $source -OutputRoot $fixture | Out-Null
 if([IO.File]::ReadAllText($header) -cne $before){throw 'Compatibility generation is nondeterministic.'}
 $script:compatChecks++
 [IO.File]::AppendAllText($header,'stale')
 Reject { & $generator -SourcePath $source -OutputRoot $fixture -Check } 'a stale language binding'
 & $generator -SourcePath $source -OutputRoot $fixture | Out-Null
 $profile=Get-Content -LiteralPath $original -Raw | ConvertFrom-Json -AsHashtable
 $profile.native.TestOverflow=@{type='u32';value='0x100000000'};Save-Profile $profile
 Reject { & $generator -SourcePath $source -OutputRoot $fixture } 'integer overflow'
 $profile.native.Remove('TestOverflow')
 $profile.native.TestInvalidHex=@{type='u64';value='0x123z'};Save-Profile $profile
 Reject { & $generator -SourcePath $source -OutputRoot $fixture } 'invalid hexadecimal input'
 $profile.native.Remove('TestInvalidHex')
 $profile.managed.GObjectsRva=@{type='u64';value=1};Save-Profile $profile
 Reject { & $generator -SourcePath $source -OutputRoot $fixture } 'duplicate cross-language constant names'
 $profile.managed.Remove('GObjectsRva')
 $profile.runtime_layout.bad_reference='@MissingConstant';Save-Profile $profile
 Reject { & $generator -SourcePath $source -OutputRoot $fixture } 'an unresolved deployment reference'
 $profile.runtime_layout.Remove('bad_reference')
 $profile.native['Bad;Injected']=@{type='u32';value=1};Save-Profile $profile
 Reject { & $generator -SourcePath $source -OutputRoot $fixture } 'a code-injecting constant name'
 Write-Output ('PASS: {0} compatibility generation checks; no game access.' -f $script:compatChecks)
}finally{
 $tempRoot=[IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\','/')+[IO.Path]::DirectorySeparatorChar
 if(-not $fixture.StartsWith($tempRoot,[StringComparison]::OrdinalIgnoreCase)){throw 'Fixture cleanup escaped the temporary directory.'}
 if(Test-Path -LiteralPath $fixture){Remove-Item -LiteralPath $fixture -Recurse -Force}
}
