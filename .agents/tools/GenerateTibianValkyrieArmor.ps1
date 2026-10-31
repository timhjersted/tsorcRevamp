<#
.SYNOPSIS
Converts the Tibian Valkyrie NPC outfit onto the working Black Knight equipment template.
.DESCRIPTION
Lifts the original hair, face, chest, skirt and pauldrons from NPC frame zero.
Recolors the working template arms to bright NPC silver. Lifts the NPC's own
twenty mapped knee/boot poses (NPC 6 idle, 0 jump, 1..14 walk), preserving
their pixels and normal frame phase. Fits the purple waist to twelve pixels.
The hair and face are a complete vanity headpiece. The NPC and weapon are unchanged.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)] [string]$RepoRoot,
    [string]$OutputDirectory,
    [string]$LegsTemplatePath
)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
if (-not $OutputDirectory) { $OutputDirectory=Join-Path $RepoRoot 'Content\Items\Armor\TibianValkyrieSet' }
$OutputDirectory=[IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
$baseRoot=Join-Path $RepoRoot 'Content\Items\Armor\BlackKnightSet'
$npc=[Drawing.Bitmap]::FromFile((Join-Path $RepoRoot 'NPCs\Enemies\TibianValkyrie.png'))
$bodyPalette=@{
    '0C0B0C'='201C1C'; '15121D'='3D3336'; '494B81'='7D727B'
    '5C5F9B'='9C8F9B'; '152433'='3D3336'; '253E63'='7D727B'
    '1C2D48'='3D3336'; '606690'='D4C7D4'; '161E35'='281E2C'
    '7E81AF'='DDD2DD'; '060A13'='201C1C'; '222222'='242131'
}
$legsPalette=@{
    '323232'='482C25'; 'EBEBEB'='F2A38E'; 'D7D7D7'='CD806B'
    'BEBEBE'='A96A59'; 'FFFFFF'='FFB6A3'
    '14161C'='281E2C'; '313545'='4A3A52'; '383D4F'='5B4B63'; '242733'='3D3044'
}
$headColors=@('260520','400A2D','650738','8D0052','C91E4A','F4445A','490B2F','C91E1E','F2A38E','CD806B','FFB6A3','C57B67','A96A59','482C25')
$metalColors=@('201C1C','3D3336','7D727B','9C8F9B','D4C7D4','DDD2DD','DFDCDC','E7E0E0','281E2C','242131')

function Set-ArmorBlock {
    param([Drawing.Bitmap]$Bitmap,[int]$X,[int]$Y,[Drawing.Color]$Color)
    $Bitmap.SetPixel($X,$Y,$Color)
    $Bitmap.SetPixel($X+1,$Y,$Color)
    $Bitmap.SetPixel($X,$Y+1,$Color)
    $Bitmap.SetPixel($X+1,$Y+1,$Color)
}

function New-RecoloredTemplate {
    param([string]$Path,[hashtable]$Palette)
    $source=[Drawing.Bitmap]::FromFile($Path)
    $result=[Drawing.Bitmap]::new($source.Width,$source.Height,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $colors=@{}
    foreach ($key in $Palette.Keys) { $colors[$key]=[Drawing.ColorTranslator]::FromHtml('#'+$Palette[$key]) }
    try {
        for ($y=0;$y -lt $source.Height;$y+=2) {
            for ($x=0;$x -lt $source.Width;$x+=2) {
                $pixel=$source.GetPixel($x,$y)
                if ($pixel.A -eq 0) { continue }
                $key='{0:X2}{1:X2}{2:X2}' -f $pixel.R,$pixel.G,$pixel.B
                if (-not $colors.ContainsKey($key)) { throw "Unmapped template color: $key" }
                Set-ArmorBlock -Bitmap $result -X $x -Y $y -Color $colors[$key]
            }
        }
    }
    finally { $source.Dispose() }
    return ,$result
}

function New-SourcePart {
    param([Drawing.Rectangle]$Crop,[Drawing.Point]$At,[switch]$Head,[switch]$Metal)
    $part=[Drawing.Bitmap]::new(40,56,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
    for ($y=$Crop.Y;$y -lt $Crop.Bottom;$y+=2) {
        for ($x=$Crop.X;$x -lt $Crop.Right;$x+=2) {
            $color=$npc.GetPixel($x,$y)
            if ($color.A -eq 0) { continue }
            $key='{0:X2}{1:X2}{2:X2}' -f $color.R,$color.G,$color.B
            if ($Head) {
                # Keep the NPC's eye white, while excluding the axe's gray edge at the crop's left side.
                $eye=$x -eq 38 -and $y -in @(22,24) -and $key -eq 'F1F1F1'
                if ($key -notin $headColors -and -not $eye) { continue }
            }
            if ($Metal -and $key -notin $metalColors) { continue }
            $destinationX=$At.X+$Crop.Right-$x-2
            $destinationY=$At.Y+$y-$Crop.Y
            $color=[Drawing.Color]::FromArgb(255,$color.R,$color.G,$color.B)
            Set-ArmorBlock -Bitmap $part -X $destinationX -Y $destinationY -Color $color
        }
    }
    return ,$part
}

function Write-ItemIcon {
    param([string]$Name,[string]$Suffix)
    $sheet=[Drawing.Bitmap]::FromFile((Join-Path $OutputDirectory "${Name}_${Suffix}.png"))
    try {
        if ($Suffix -eq 'Body') {
            $composite=[Drawing.Bitmap]::new(40,56,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
            $graphics=[Drawing.Graphics]::FromImage($composite)
            $graphics.InterpolationMode='NearestNeighbor'
            $graphics.PixelOffsetMode='Half'
            foreach ($cell in @(@(1,1),@(0,0),@(0,1))) {
                $graphics.DrawImage($sheet,[Drawing.Rectangle]::new(0,0,40,56),$cell[0]*40,$cell[1]*56,40,56,[Drawing.GraphicsUnit]::Pixel)
            }
            $graphics.Dispose()
            $sheet.Dispose()
            $sheet=$composite
        }
        $left=40; $top=56; $right=0; $bottom=0
        for ($y=0;$y -lt 56;$y++) {
            for ($x=0;$x -lt 40;$x++) {
                if ($sheet.GetPixel($x,$y).A -gt 0) {
                    $left=[Math]::Min($left,$x); $top=[Math]::Min($top,$y)
                    $right=[Math]::Max($right,$x+1); $bottom=[Math]::Max($bottom,$y+1)
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

function New-NpcLegs {
    # This NPC and the player both use the authored fourteen-frame fighter walk.
    # NPC 0 is airborne, NPC 6 is idle; NPC 1..14 match player 6..19.
    $result=[Drawing.Bitmap]::new(40,1120,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $legColors=@('281E2C','4A3A52','5B4B63','3D3044','F2A38E','CD806B','482C25','3C1D16','FFB6A3','A96A59')
    for($frame=0;$frame -lt 20;$frame++) {
        $sourceFrame=6
        if($frame -eq 5){$sourceFrame=0}
        elseif($frame -ge 6){$sourceFrame=$frame-5}
        for($y=42;$y -lt 58;$y+=2) {
            for($x=20;$x -lt 60;$x+=2) {
                $color=$npc.GetPixel($x,$sourceFrame*58+$y)
                if(-not $color.A){continue}
                $key='{0:X2}{1:X2}{2:X2}' -f $color.R,$color.G,$color.B
                if($key -in $legColors){Set-ArmorBlock $result (60-$x) ($frame*56+$y-2) $color}
            }
        }
    }
    return ,$result
}

if ($npc.Width -ne 76 -or $npc.Height -ne 870) { throw 'Expected the original 76x870 Tibian Valkyrie sheet.' }
$headPart=New-SourcePart -Crop ([Drawing.Rectangle]::new(28,4,32,26)) -At ([Drawing.Point]::new(2,2)) -Head
$frontCap=New-SourcePart -Crop ([Drawing.Rectangle]::new(44,26,10,8)) -At ([Drawing.Point]::new(8,24)) -Metal
$backCap=New-SourcePart -Crop ([Drawing.Rectangle]::new(46,26,6,6)) -At ([Drawing.Point]::new(26,26)) -Metal
$chest=New-SourcePart -Crop ([Drawing.Rectangle]::new(32,30,14,10)) -At ([Drawing.Point]::new(16,28))
$skirt=New-SourcePart -Crop ([Drawing.Rectangle]::new(30,40,20,6)) -At ([Drawing.Point]::new(12,38))
# Fit the waist beneath the chest: remove eight native pixels of excess width,
# retaining the source strip's color groups and two-pixel outline blocks.
$fitted=[Drawing.Bitmap]::new(40,56,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
$columns=@(0,2,4,5,7,9)
for($y=38;$y -lt 44;$y+=2){for($i=0;$i -lt 6;$i++){Set-ArmorBlock $fitted (16+$i*2) $y ($skirt.GetPixel(12+$columns[$i]*2,$y))}}
$skirt.Dispose();$skirt=$fitted
$body=New-RecoloredTemplate -Path (Join-Path $baseRoot 'BlackKnightBreastplate_Body.png') -Palette $bodyPalette
$legs=if($LegsTemplatePath){New-RecoloredTemplate -Path $LegsTemplatePath -Palette $legsPalette}else{New-NpcLegs}
$head=[Drawing.Bitmap]::new(40,1120,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
$referenceHead=[Drawing.Bitmap]::FromFile((Join-Path $baseRoot 'BlackKnightHelmet_Head.png'))
$headGraphics=[Drawing.Graphics]::FromImage($head)
$bodyGraphics=[Drawing.Graphics]::FromImage($body)
try {
    $bodyGraphics.InterpolationMode='NearestNeighbor'; $bodyGraphics.PixelOffsetMode='Half'
    foreach ($torsoRow in @(0,2)) {
        foreach ($torsoColumn in @(0,1)) {
            $bodyGraphics.DrawImageUnscaled($chest,$torsoColumn*40,$torsoRow*56)
            # Replace the inherited long waist rather than layering a second strip over it.
            # NPC rows 40-45 map two pixels higher to fit the 56px player frame.
            # Source knees and boots use the same translation in every pose.
            $bodyGraphics.CompositingMode='SourceCopy'
            $bodyGraphics.FillRectangle([Drawing.Brushes]::Transparent,$torsoColumn*40,$torsoRow*56+38,40,18)
            $bodyGraphics.CompositingMode='SourceOver'
            $bodyGraphics.DrawImageUnscaled($skirt,$torsoColumn*40,$torsoRow*56)
        }
        $bodyGraphics.DrawImageUnscaled($frontCap,0,($torsoRow+1)*56)
        $bodyGraphics.DrawImageUnscaled($backCap,40,($torsoRow+1)*56)
    }
    $bodyGraphics.Dispose()

    # The new fixed cap carries the pauldron; leave dark padding in the rotating upper arm.
    $padding=[Drawing.ColorTranslator]::FromHtml('#3D3336')
    for ($stretch=0;$stretch -lt 4;$stretch++) {
        foreach ($column in @(7,8)) {
            $top=56
            for ($y=0;$y -lt 56;$y+=2) {
                for ($x=0;$x -lt 40;$x+=2) {
                    if ($body.GetPixel($column*40+$x,$stretch*56+$y).A -gt 0) { $top=[Math]::Min($top,$y) }
                }
            }
            for ($y=$top;$y -lt $top+6;$y+=2) {
                for ($x=0;$x -lt 40;$x+=2) {
                    $pixel=$body.GetPixel($column*40+$x,$stretch*56+$y)
                    if ($pixel.A -eq 0 -or $pixel.ToArgb() -eq [Drawing.ColorTranslator]::FromHtml('#201C1C').ToArgb()) { continue }
                    Set-ArmorBlock -Bitmap $body -X ($column*40+$x) -Y ($stretch*56+$y) -Color $padding
                }
            }
        }
    }

    # Use the source gauntlet's bright silver values throughout every normal and
    # composite arm pose. Preserve the template's outline, elbow and hand masks.
    $gauntletColors=@{'7D727B'='9C8F9B';'9C8F9B'='D4C7D4';'D4C7D4'='DDD2DD'}
    for($y=0;$y -lt 224;$y+=2){for($x=80;$x -lt 360;$x+=2){
        $color=$body.GetPixel($x,$y);if(-not $color.A){continue}
        $key='{0:X2}{1:X2}{2:X2}' -f $color.R,$color.G,$color.B
        if($gauntletColors.ContainsKey($key)){Set-ArmorBlock $body $x $y ([Drawing.ColorTranslator]::FromHtml('#'+$gauntletColors[$key]))}
    }}

    $idleTop=56
    for ($frame=0;$frame -lt 20;$frame++) {
        $referenceTop=56
        for ($y=0;$y -lt 56;$y+=2) {
            for ($x=0;$x -lt 40;$x+=2) {
                if ($referenceHead.GetPixel($x,$frame*56+$y).A -gt 0) { $referenceTop=[Math]::Min($referenceTop,$y) }
            }
        }
        if ($frame -eq 0) { $idleTop=$referenceTop }
        $headGraphics.DrawImageUnscaled($headPart,0,$frame*56+$referenceTop-$idleTop)
    }
    $head.Save((Join-Path $OutputDirectory 'TibianValkyrieHeadpiece_Head.png'),[Drawing.Imaging.ImageFormat]::Png)
    $body.Save((Join-Path $OutputDirectory 'TibianValkyrieBreastplate_Body.png'),[Drawing.Imaging.ImageFormat]::Png)
    $legs.Save((Join-Path $OutputDirectory 'TibianValkyrieGreaves_Legs.png'),[Drawing.Imaging.ImageFormat]::Png)
}
finally {
    $headGraphics.Dispose(); $bodyGraphics.Dispose()
    foreach ($bitmap in @($npc,$headPart,$frontCap,$backCap,$chest,$skirt,$body,$legs,$head,$referenceHead)) { $bitmap.Dispose() }
}
Write-ItemIcon -Name TibianValkyrieHeadpiece -Suffix Head
Write-ItemIcon -Name TibianValkyrieBreastplate -Suffix Body
Write-ItemIcon -Name TibianValkyrieGreaves -Suffix Legs
