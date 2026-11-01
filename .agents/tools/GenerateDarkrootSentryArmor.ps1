<#
.SYNOPSIS
Recolors the working Black Knight armor template for the Darkroot Sentry.
.DESCRIPTION
Preserves the arm cells and leg poses from BlackKnightSet, with a small filled
collar, horizontal chest trim and darker walking-glove edges. The approved custom
cyclops helmet follows the template's frame offsets, and the green cloth is a
fixed overlay in every leg frame. No rotations or procedural walk poses are used.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$RepoRoot,
    [string]$OutputDirectory,
    [string]$ComponentsPath
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
if (-not $OutputDirectory) {
    $OutputDirectory = Join-Path $RepoRoot 'Content\Items\Armor\Enemy\DarkrootSentrySet'
}
if (-not $ComponentsPath) {
    $ComponentsPath = Join-Path $PSScriptRoot 'assets\DarkrootSentry-components.png'
}
$OutputDirectory = [IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
$referenceDirectory = Join-Path $RepoRoot 'Content\Items\Armor\Enemy\BlackKnightSet'

$bodyPalette = @{
    '0C0B0C' = '111416'; '15121D' = '202627'; '494B81' = '343D3C'
    '5C5F9B' = '4E5752'; '152433' = '202627'; '253E63' = '514833'
    '1C2D48' = '343D3C'; '606690' = '82734D'; '161E35' = '202627'
    '7E81AF' = 'AB9A67'; '060A13' = '111416'; '222222' = '283226'
}
$legsPalette = @{
    '070614' = '111416'; '0E122C' = '202627'; '131939' = '343D3C'
    '141741' = '4E5752'; '1C2A45' = '514833'; '2A3758' = '82734D'
    '34495E' = '79817A'
}

function New-RecoloredTemplate {
    param([string]$Path, [hashtable]$Palette)

    $source = [Drawing.Bitmap]::FromFile($Path)
    $result = [Drawing.Bitmap]::new($source.Width, $source.Height, [Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $colors = @{}
    foreach ($key in $Palette.Keys) {
        $colors[$key] = [Drawing.ColorTranslator]::FromHtml('#' + $Palette[$key])
    }
    try {
        for ($y = 0; $y -lt $source.Height; $y += 2) {
            for ($x = 0; $x -lt $source.Width; $x += 2) {
                $pixel = $source.GetPixel($x, $y)
                if ($pixel.A -eq 0) { continue }
                $key = '{0:X2}{1:X2}{2:X2}' -f $pixel.R, $pixel.G, $pixel.B
                if (-not $colors.ContainsKey($key)) { throw "Unmapped template color $key in $Path" }
                $color = [Drawing.Color]::FromArgb($pixel.A, $colors[$key])
                $result.SetPixel($x, $y, $color)
                $result.SetPixel($x + 1, $y, $color)
                $result.SetPixel($x, $y + 1, $color)
                $result.SetPixel($x + 1, $y + 1, $color)
            }
        }
    }
    finally {
        $source.Dispose()
    }
    return ,$result
}

function Set-ArmorBlock {
    param([Drawing.Bitmap]$Bitmap, [int]$X, [int]$Y, [Drawing.Color]$Color)

    $Bitmap.SetPixel($X,$Y,$Color)
    $Bitmap.SetPixel($X+1,$Y,$Color)
    $Bitmap.SetPixel($X,$Y+1,$Color)
    $Bitmap.SetPixel($X+1,$Y+1,$Color)
}

function Write-ItemIcon {
    param([string]$PartName, [string]$Suffix)

    $sheet = [Drawing.Bitmap]::FromFile((Join-Path $OutputDirectory "DarkrootSentry${PartName}_${Suffix}.png"))
    try {
        $left = 40
        $top = 56
        $right = 0
        $bottom = 0
        for ($y = 0; $y -lt 56; $y++) {
            for ($x = 0; $x -lt 40; $x++) {
                if ($sheet.GetPixel($x, $y).A -gt 0) {
                    $left = [Math]::Min($left, $x)
                    $top = [Math]::Min($top, $y)
                    $right = [Math]::Max($right, $x + 1)
                    $bottom = [Math]::Max($bottom, $y + 1)
                }
            }
        }
        $bounds = [Drawing.Rectangle]::new($left, $top, $right - $left, $bottom - $top)
        $icon = $sheet.Clone($bounds, [Drawing.Imaging.PixelFormat]::Format32bppArgb)
        try {
            $icon.Save((Join-Path $OutputDirectory "DarkrootSentry${PartName}.png"), [Drawing.Imaging.ImageFormat]::Png)
        }
        finally { $icon.Dispose() }
    }
    finally { $sheet.Dispose() }
}

$atlas = [Drawing.Bitmap]::FromFile([IO.Path]::GetFullPath($ComponentsPath))
$referenceHead = [Drawing.Bitmap]::FromFile((Join-Path $referenceDirectory 'BlackKnightHelmet_Head.png'))
$body = New-RecoloredTemplate -Path (Join-Path $referenceDirectory 'BlackKnightBreastplate_Body.png') -Palette $bodyPalette
$legs = New-RecoloredTemplate -Path (Join-Path $referenceDirectory 'BlackKnightGreaves_Legs.png') -Palette $legsPalette
$head = [Drawing.Bitmap]::new(40, 1120, [Drawing.Imaging.PixelFormat]::Format32bppArgb)
$headGraphics = [Drawing.Graphics]::FromImage($head)
$legsGraphics = [Drawing.Graphics]::FromImage($legs)
try {
    if ($atlas.Width -ne 360 -or $atlas.Height -ne 56) { throw 'Expected the 360x56 Sentry component atlas.' }

    # Close the small helmet-to-cuirass gap without enlarging the approved helmet.
    # Coordinates are 2x2 blocks within a torso cell; keep gender and jump variants consistent.
    $collar = @(
        @(8,11,'202627'), @(9,11,'343D3C'), @(10,11,'343D3C'), @(11,11,'202627'),
        @(8,12,'202627'), @(9,12,'343D3C'), @(10,12,'4E5752'), @(11,12,'343D3C'), @(12,12,'202627'),
        @(8,13,'343D3C'), @(9,13,'4E5752'), @(10,13,'4E5752'), @(11,13,'4E5752'), @(12,13,'343D3C')
    )
    $goldToSteel = @{
        '514833' = '202627'; '82734D' = '4E5752'; 'AB9A67' = '343D3C'
    }
    $chestLine = @('514833','82734D','82734D','82734D','AB9A67','514833')
    foreach ($torsoRow in @(0,2)) {
        foreach ($torsoColumn in @(0,1)) {
            $originX = $torsoColumn * 40
            $originY = $torsoRow * 56
            foreach ($block in $collar) {
                $color = [Drawing.ColorTranslator]::FromHtml('#' + $block[2])
                Set-ArmorBlock -Bitmap $body -X ($originX+$block[0]*2) -Y ($originY+$block[1]*2) -Color $color
            }

            # Replace the clustered chest glints with a single aged-gold horizontal band.
            for ($y = 15; $y -le 19; $y++) {
                for ($x = 7; $x -le 13; $x++) {
                    $pixel = $body.GetPixel($originX+$x*2,$originY+$y*2)
                    $key = '{0:X2}{1:X2}{2:X2}' -f $pixel.R,$pixel.G,$pixel.B
                    if ($pixel.A -eq 0 -or -not $goldToSteel.ContainsKey($key)) { continue }
                    $color = [Drawing.ColorTranslator]::FromHtml('#' + $goldToSteel[$key])
                    Set-ArmorBlock -Bitmap $body -X ($originX+$x*2) -Y ($originY+$y*2) -Color $color
                }
            }
            for ($index = 0; $index -lt $chestLine.Count; $index++) {
                $color = [Drawing.ColorTranslator]::FromHtml('#' + $chestLine[$index])
                Set-ArmorBlock -Bitmap $body -X ($originX+(7+$index)*2) -Y ($originY+34) -Color $color
            }
        }
    }

    # Darken only the exposed glove edges in the four bent-elbow walking cells.
    # Preserve alpha and interior shading so the established wrist and elbow poses stay intact.
    $outline = [Drawing.ColorTranslator]::FromHtml('#111416')
    foreach ($armRow in @(1,3)) {
        foreach ($armColumn in @(3,4,5,6)) {
            $originX = $armColumn * 40
            $originY = $armRow * 56
            for ($y = 16; $y -le 19; $y++) {
                for ($x = 1; $x -lt 19; $x++) {
                    $pixel = $body.GetPixel($originX+$x*2,$originY+$y*2)
                    if ($pixel.A -eq 0 -or $pixel.R -ne 32 -or $pixel.G -ne 38 -or $pixel.B -ne 39) { continue }
                    $edge = $false
                    foreach ($neighbor in @(@(-1,0),@(1,0),@(0,-1),@(0,1))) {
                        if ($body.GetPixel($originX+($x+$neighbor[0])*2,$originY+($y+$neighbor[1])*2).A -eq 0) { $edge = $true }
                    }
                    if (-not $edge) { continue }
                    Set-ArmorBlock -Bitmap $body -X ($originX+$x*2) -Y ($originY+$y*2) -Color $outline
                }
            }
        }
    }

    $headGraphics.InterpolationMode = 'NearestNeighbor'
    $headGraphics.PixelOffsetMode = 'Half'
    $legsGraphics.InterpolationMode = 'NearestNeighbor'
    $legsGraphics.PixelOffsetMode = 'Half'
    $idleTop = 56

    for ($frame = 0; $frame -lt 20; $frame++) {
        $frameTop = $frame * 56
        $referenceTop = 56
        for ($y = 0; $y -lt 56; $y += 2) {
            for ($x = 0; $x -lt 40; $x += 2) {
                if ($referenceHead.GetPixel($x, $frameTop + $y).A -gt 0) {
                    $referenceTop = [Math]::Min($referenceTop, $y)
                }
            }
        }
        if ($frame -eq 0) { $idleTop = $referenceTop }
        $headOffset = $referenceTop - $idleTop
        $headGraphics.DrawImage($atlas, [Drawing.Rectangle]::new(0, $frameTop + $headOffset, 40, 56), 0, 0, 40, 56, [Drawing.GraphicsUnit]::Pixel)

        # The cloth is rigid decoration. Copy this exact cell, without displacement or deformation.
        $legsGraphics.DrawImage($atlas, [Drawing.Rectangle]::new(0, $frameTop, 40, 56), 320, 0, 40, 56, [Drawing.GraphicsUnit]::Pixel)
    }
    $head.Save((Join-Path $OutputDirectory 'DarkrootSentryHelmet_Head.png'), [Drawing.Imaging.ImageFormat]::Png)
    $body.Save((Join-Path $OutputDirectory 'DarkrootSentryBreastplate_Body.png'), [Drawing.Imaging.ImageFormat]::Png)
    $legs.Save((Join-Path $OutputDirectory 'DarkrootSentryGreaves_Legs.png'), [Drawing.Imaging.ImageFormat]::Png)
}
finally {
    $headGraphics.Dispose()
    $legsGraphics.Dispose()
    $head.Dispose()
    $body.Dispose()
    $legs.Dispose()
    $referenceHead.Dispose()
    $atlas.Dispose()
}

Write-ItemIcon -PartName Helmet -Suffix Head
Write-ItemIcon -PartName Breastplate -Suffix Body
Write-ItemIcon -PartName Greaves -Suffix Legs
Write-Output "Generated template-based Darkroot Sentry armor in $OutputDirectory"
