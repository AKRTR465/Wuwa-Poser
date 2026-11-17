$catalogDigests = [Collections.Generic.Dictionary[string, string]]::new([StringComparer]::Ordinal)
$catalogJson = [Collections.Generic.Dictionary[string, object]]::new([StringComparer]::Ordinal)

function Get-CatalogFileDigest([string]$Path) {
    $fullPath = [IO.Path]::GetFullPath($Path)
    if (-not $catalogDigests.ContainsKey($fullPath)) {
        $catalogDigests[$fullPath] = (Get-FileHash -LiteralPath $fullPath -Algorithm SHA256).Hash.ToLowerInvariant()
    }
    return $catalogDigests[$fullPath]
}

function Read-CatalogJsonWithDigest([string]$Path) {
    $fullPath = [IO.Path]::GetFullPath($Path)
    if (-not $catalogJson.ContainsKey($fullPath)) {
        $bytes = [IO.File]::ReadAllBytes($fullPath)
        $sha = [Security.Cryptography.SHA256]::Create()
        try { $digest = [BitConverter]::ToString($sha.ComputeHash($bytes)).Replace('-', '').ToLowerInvariant() }
        finally { $sha.Dispose() }
        $catalogDigests[$fullPath] = $digest
        $json = [Text.Encoding]::UTF8.GetString($bytes).TrimStart([char]0xFEFF) | ConvertFrom-Json
        $catalogJson[$fullPath] = [PSCustomObject]@{ Json = $json; Sha256 = $digest }
    }
    return $catalogJson[$fullPath]
}
