# Renders the noise texture pack as a grayscale contact sheet of R * A / 255 — what the shader
# actually sees after tModLoader premultiplies — so textures get picked by LOOKING at them rather
# than by filename. Several textures in the pack are not what their names suggest.
#
#   .\ContactSheet.ps1                     # every texture, single tile
#   .\ContactSheet.ps1 -Tile2x2            # 2x2, to check the seam
#   .\ContactSheet.ps1 -Names a,b,c -Tile2x2
#
# ALWAYS run the -Tile2x2 pass before committing to a texture. Every one of these shaders scrolls
# its samples, so a texture with a visible seam is unusable no matter how good the single tile looks.
# That check is what eliminated T_LiquidVertical_Wave, T_Wave43 and T_FirePanningCyl45 from the Nito
# kit: all three are genuinely flame-shaped, and all three carry a hard horizontal baseline that
# strobes past every time you scroll them vertically.

param(
    [string[]] $Names,
    [switch]   $Tile2x2,
    [string]   $Out  = "contact.png",
    [int]      $Cell = 150,
    [int]      $Cols = 7
)

Add-Type -AssemblyName System.Drawing
$root = Join-Path $PSScriptRoot "..\..\..\..\Textures\Noise"

if (-not $Names) {
    $Names = Get-ChildItem -Path $root -Filter *.png | ForEach-Object { $_.BaseName } | Sort-Object
}
# `powershell -File script.ps1 -Names a,b,c` from a non-PowerShell shell (Git Bash, the Bash tool)
# arrives as ONE string rather than an array. Split defensively so both call styles work.
$Names = @($Names | ForEach-Object { $_ -split ',' } | Where-Object { $_ })

$reps = if ($Tile2x2) { 2.0 } else { 1.0 }
$rows = [math]::Ceiling($Names.Count / $Cols)
$sheet = New-Object System.Drawing.Bitmap (($Cell * $Cols), ($rows * ($Cell + 14)))
$g = [System.Drawing.Graphics]::FromImage($sheet)
$g.Clear([System.Drawing.Color]::FromArgb(20, 20, 30))
$font = New-Object System.Drawing.Font("Consolas", 8)

for ($i = 0; $i -lt $Names.Count; $i++) {
    $path = Join-Path $root ($Names[$i] + ".png")
    if (-not (Test-Path $path)) { Write-Warning "missing: $($Names[$i])"; continue }
    $img = New-Object System.Drawing.Bitmap $path
    $tile = New-Object System.Drawing.Bitmap $Cell, $Cell
    for ($y = 0; $y -lt $Cell; $y++) {
        for ($x = 0; $x -lt $Cell; $x++) {
            $u = ($x * $reps / $Cell) % 1.0
            $v = ($y * $reps / $Cell) % 1.0
            $px = $img.GetPixel([int]($u * ($img.Width - 1)), [int]($v * ($img.Height - 1)))
            $val = [int]($px.R * $px.A / 255)      # exactly what tex2D(...).r returns
            $tile.SetPixel($x, $y, [System.Drawing.Color]::FromArgb(255, $val, $val, $val))
        }
    }
    $cx = ($i % $Cols) * $Cell
    $cy = [math]::Floor($i / $Cols) * ($Cell + 14)
    $g.DrawImage($tile, $cx, $cy, $Cell, $Cell)
    $g.DrawString($Names[$i], $font, [System.Drawing.Brushes]::Yellow, $cx, ($cy + $Cell))
    $img.Dispose(); $tile.Dispose()
}

$sheet.Save((Join-Path $PSScriptRoot $Out))
Write-Output "wrote $Out ($($Names.Count) textures, $(if ($Tile2x2) {'2x2 tiled'} else {'single tile'}))"
