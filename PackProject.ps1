# ─── PackProject.ps1 ────────────────────────────────────────────────────────────
$zipOut = "TWL_CodeOnly.zip"

$includeExt = @(
    '.cs','.csproj','.sln',
    '.json','.xml','.yaml','.yml',
    '.shader','.hlsl','.cginc','.glsl',
    '.txt','.md'
)

$exclude = @(
    "$PWD\**\bin\*",
    "$PWD\**\obj\*",
    "$PWD\.vs\*",
    "$PWD\.git\*",
    "$PWD\packages\*",
    "$PWD\**\*.user",
    "$PWD\**\*.cache"
)

# --- Recopilar rutas -----------------------------------------------------------
$files = Get-ChildItem -Recurse -File | Where-Object {
    $path = $_.FullName
    ($includeExt -contains $_.Extension.ToLower()) -and
    (-not ($exclude | Where-Object { $path -like $_ }))
} | Select-Object -ExpandProperty FullName   # ← convierte a string

Write-Host "Archivos encontrados: $($files.Count)"

if ($files.Count -eq 0)
{
    Write-Warning "No se encontraron archivos que cumplan los criterios. Nada que comprimir."
    exit
}

# --- Comprimir -----------------------------------------------------------------
Compress-Archive -Path $files -DestinationPath $zipOut -CompressionLevel Optimal -Force

Write-Host "`n► Código comprimido en: $zipOut"
exit
# ───────────────────────────────────────────────────────────────────────────────
