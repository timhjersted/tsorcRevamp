<#
.SYNOPSIS
Builds the existing SentientSet equipment textures from the working Black Knight template.
.DESCRIPTION
Keeps the established walking arms, feet, composite stretches and head bob.
Custom art adds a closed multi-eyed helmet, dark collar, chest eye and fixed
purple cloth. The component atlas has four 40x56 cells: helmet, cloth, front
shoulder cap and rear shoulder cap. The caps cover dark upper-arm padding.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)] [string]$RepoRoot,
    [string]$OutputDirectory,
    [string]$ComponentsPath
)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
if (-not $OutputDirectory) { $OutputDirectory = Join-Path $RepoRoot 'Content\Items\Armor\SentientSet' }
if (-not $ComponentsPath) { $ComponentsPath = Join-Path $PSScriptRoot 'assets\SentientSet-components.png' }
$OutputDirectory = [IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
$referenceDirectory = Join-Path $RepoRoot 'Content\Items\Armor\BlackKnightSet'
$bodyPalette = @{
    '0C0B0C'='0B0C10'; '15121D'='181A20'; '494B81'='2E313A'
    '5C5F9B'='4A4E59'; '152433'='181A20'; '253E63'='4A4E59'
    '1C2D48'='2E313A'; '606690'='5B3F73'; '161E35'='181A20'
    '7E81AF'='777D88'; '060A13'='0B0C10'; '222222'='30233E'
}
$legsPalette = @{
    '070614'='0B0C10'; '0E122C'='181A20'; '131939'='2E313A'
    '141741'='4A4E59'; '1C2A45'='30233E'; '2A3758'='5B3F73'
    '34495E'='777D88'
}

function Set-ArmorBlock {
    param([Drawing.Bitmap]$Bitmap,[int]$X,[int]$Y,[Drawing.Color]$Color)
    $Bitmap.SetPixel($X,$Y,$Color)
    $Bitmap.SetPixel($X+1,$Y,$Color)
    $Bitmap.SetPixel($X,$Y+1,$Color)
    $Bitmap.SetPixel($X+1,$Y+1,$Color)
}

function New-RecoloredTemplate {
    param([string]$Path,[hashtable]$Palette)
    $source = [Drawing.Bitmap]::FromFile($Path)
    $result = [Drawing.Bitmap]::new($source.Width,$source.Height,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $colors = @{}
    foreach ($key in $Palette.Keys) { $colors[$key] = [Drawing.ColorTranslator]::FromHtml('#'+$Palette[$key]) }
    try {
        for ($y=0; $y -lt $source.Height; $y+=2) {
            for ($x=0; $x -lt $source.Width; $x+=2) {
                $pixel = $source.GetPixel($x,$y)
                if ($pixel.A -eq 0) { continue }
                $key = '{0:X2}{1:X2}{2:X2}' -f $pixel.R,$pixel.G,$pixel.B
                if (-not $colors.ContainsKey($key)) { throw "Unmapped template color: $key" }
                Set-ArmorBlock -Bitmap $result -X $x -Y $y -Color $colors[$key]
            }
        }
    }
    finally { $source.Dispose() }
    return ,$result
}

function Write-ItemIcon {
    param([string]$Name,[string]$Suffix)
    $sheet = [Drawing.Bitmap]::FromFile((Join-Path $OutputDirectory "${Name}_${Suffix}.png"))
    try {
        if ($Suffix -eq 'Body') {
            $composite = [Drawing.Bitmap]::new(40,56,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
            $graphics = [Drawing.Graphics]::FromImage($composite)
            $graphics.InterpolationMode = 'NearestNeighbor'
            $graphics.PixelOffsetMode = 'Half'
            foreach ($cell in @(@(1,1),@(0,0),@(0,1))) {
                $graphics.DrawImage($sheet,[Drawing.Rectangle]::new(0,0,40,56),$cell[0]*40,$cell[1]*56,40,56,[Drawing.GraphicsUnit]::Pixel)
            }
            $graphics.Dispose()
            $sheet.Dispose()
            $sheet = $composite
        }
        $left=40; $top=56; $right=0; $bottom=0
        for ($y=0; $y -lt 56; $y++) {
            for ($x=0; $x -lt 40; $x++) {
                if ($sheet.GetPixel($x,$y).A -gt 0) {
                    $left=[Math]::Min($left,$x)
                    $top=[Math]::Min($top,$y)
                    $right=[Math]::Max($right,$x+1)
                    $bottom=[Math]::Max($bottom,$y+1)
                }
            }
        }
        $icon=$sheet.Clone([Drawing.Rectangle]::new($left,$top,$right-$left,$bottom-$top),[Drawing.Imaging.PixelFormat]::Format32bppArgb)
        try { $icon.Save((Join-Path $OutputDirectory "$Name.png"),[Drawing.Imaging.ImageFormat]::Png) }
        finally { $icon.Dispose() }
        Write-Output "$Name icon: $($right-$left)x$($bottom-$top)"
    }
    finally { $sheet.Dispose() }
}

$atlas = [Drawing.Bitmap]::FromFile([IO.Path]::GetFullPath($ComponentsPath))
$referenceHead = [Drawing.Bitmap]::FromFile((Join-Path $referenceDirectory 'BlackKnightHelmet_Head.png'))
$body = New-RecoloredTemplate -Path (Join-Path $referenceDirectory 'BlackKnightBreastplate_Body.png') -Palette $bodyPalette
$legs = New-RecoloredTemplate -Path (Join-Path $referenceDirectory 'BlackKnightGreaves_Legs.png') -Palette $legsPalette
$head = [Drawing.Bitmap]::new(40,1120,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
$headGraphics = [Drawing.Graphics]::FromImage($head)
$legGraphics = [Drawing.Graphics]::FromImage($legs)
try {
    if ($atlas.Width -ne 160 -or $atlas.Height -ne 56) { throw 'Expected a 160x56 helmet/cloth/shoulder atlas.' }
    $collar = @(
        @(8,11,'181A20'),@(9,11,'2E313A'),@(10,11,'2E313A'),@(11,11,'181A20'),
        @(8,12,'181A20'),@(9,12,'2E313A'),@(10,12,'4A4E59'),@(11,12,'2E313A'),@(12,12,'181A20'),
        @(8,13,'2E313A'),@(9,13,'4A4E59'),@(10,13,'4A4E59'),@(11,13,'4A4E59'),@(12,13,'2E313A')
    )
    $chestEye = @(
        @(10,14,'30233E'),@(11,14,'5B3F73'),@(12,14,'181A20'),
        @(9,15,'0B0C10'),@(10,15,'9D7523'),@(11,15,'FFF08A'),@(12,15,'0B0C10'),
        @(10,16,'181A20'),@(11,16,'30233E'),@(12,16,'181A20')
    )
    foreach ($torsoRow in @(0,2)) {
        foreach ($torsoColumn in @(0,1)) {
            foreach ($block in ($collar+$chestEye)) {
                $color=[Drawing.ColorTranslator]::FromHtml('#'+$block[2])
                Set-ArmorBlock -Bitmap $body -X ($torsoColumn*40+$block[0]*2) -Y ($torsoRow*56+$block[1]*2) -Color $color
            }
        }
    }
    $outline=[Drawing.ColorTranslator]::FromHtml('#0B0C10')
    foreach ($armRow in @(1,3)) {
        foreach ($armColumn in @(3,4,5,6)) {
            for ($y=16; $y -le 19; $y++) {
                for ($x=1; $x -lt 19; $x++) {
                    $pixel=$body.GetPixel($armColumn*40+$x*2,$armRow*56+$y*2)
                    if ($pixel.A -eq 0 -or $pixel.R -ne 24 -or $pixel.G -ne 26 -or $pixel.B -ne 32) { continue }
                    $edge=$false
                    foreach ($neighbor in @(@(-1,0),@(1,0),@(0,-1),@(0,1))) {
                        if ($body.GetPixel($armColumn*40+($x+$neighbor[0])*2,$armRow*56+($y+$neighbor[1])*2).A -eq 0) { $edge=$true }
                    }
                    if ($edge) { Set-ArmorBlock -Bitmap $body -X ($armColumn*40+$x*2) -Y ($armRow*56+$y*2) -Color $outline }
                }
            }
        }
    }

    # The articulated arm keeps its template shape; its old plated shoulder becomes
    # dark padding so only the dedicated shoulder-cap cells carry the large pauldron.
    $padding = [Drawing.ColorTranslator]::FromHtml('#181A20')
    for ($stretch=0; $stretch -lt 4; $stretch++) {
        foreach ($armColumn in @(7,8)) {
            $top=56
            for ($y=0; $y -lt 56; $y+=2) {
                for ($x=0; $x -lt 40; $x+=2) {
                    if ($body.GetPixel($armColumn*40+$x,$stretch*56+$y).A -gt 0) { $top=[Math]::Min($top,$y) }
                }
            }
            for ($y=$top; $y -lt $top+6; $y+=2) {
                for ($x=0; $x -lt 40; $x+=2) {
                    $pixel=$body.GetPixel($armColumn*40+$x,$stretch*56+$y)
                    if ($pixel.A -eq 0 -or $pixel.ToArgb() -eq [Drawing.ColorTranslator]::FromHtml('#0B0C10').ToArgb()) { continue }
                    Set-ArmorBlock -Bitmap $body -X ($armColumn*40+$x) -Y ($stretch*56+$y) -Color $padding
                }
            }
        }
    }

    $bodyGraphics = [Drawing.Graphics]::FromImage($body)
    $bodyGraphics.InterpolationMode='NearestNeighbor'
    $bodyGraphics.PixelOffsetMode='Half'
    foreach ($shoulderRow in @(1,3)) {
        $bodyGraphics.DrawImage($atlas,[Drawing.Rectangle]::new(0,$shoulderRow*56,40,56),80,0,40,56,[Drawing.GraphicsUnit]::Pixel)
        $bodyGraphics.DrawImage($atlas,[Drawing.Rectangle]::new(40,$shoulderRow*56,40,56),120,0,40,56,[Drawing.GraphicsUnit]::Pixel)
    }
    $bodyGraphics.Dispose()

    $headGraphics.InterpolationMode='NearestNeighbor'
    $headGraphics.PixelOffsetMode='Half'
    $legGraphics.InterpolationMode='NearestNeighbor'
    $legGraphics.PixelOffsetMode='Half'
    $idleTop=56
    for ($frame=0; $frame -lt 20; $frame++) {
        $referenceTop=56
        for ($y=0; $y -lt 56; $y+=2) {
            for ($x=0; $x -lt 40; $x+=2) {
                if ($referenceHead.GetPixel($x,$frame*56+$y).A -gt 0) { $referenceTop=[Math]::Min($referenceTop,$y) }
            }
        }
        if ($frame -eq 0) { $idleTop=$referenceTop }
        $headGraphics.DrawImage($atlas,[Drawing.Rectangle]::new(0,$frame*56+$referenceTop-$idleTop,40,56),0,0,40,56,[Drawing.GraphicsUnit]::Pixel)
        $legGraphics.DrawImage($atlas,[Drawing.Rectangle]::new(0,$frame*56,40,56),40,0,40,56,[Drawing.GraphicsUnit]::Pixel)
    }
    $head.Save((Join-Path $OutputDirectory 'SentientHead_Head.png'),[Drawing.Imaging.ImageFormat]::Png)
    $body.Save((Join-Path $OutputDirectory 'SentientTorso_Body.png'),[Drawing.Imaging.ImageFormat]::Png)
    $legs.Save((Join-Path $OutputDirectory 'SentientLegs_Legs.png'),[Drawing.Imaging.ImageFormat]::Png)
}
finally {
    $headGraphics.Dispose(); $legGraphics.Dispose()
    $head.Dispose(); $body.Dispose(); $legs.Dispose()
    $referenceHead.Dispose(); $atlas.Dispose()
}
Write-ItemIcon -Name SentientHead -Suffix Head
Write-ItemIcon -Name SentientTorso -Suffix Body
Write-ItemIcon -Name SentientLegs -Suffix Legs
