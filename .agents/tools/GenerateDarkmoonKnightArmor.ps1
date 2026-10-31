<#
.SYNOPSIS
Builds Darkmoon Knight equip sheets from the Black Knight rig and original Darkmoon NPC pixels.
.EXAMPLE
.\GenerateDarkmoonKnightArmor.ps1 -RepoRoot 'C:\path\to\tsorcRevamp'
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$RepoRoot,
    [string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

if (-not $OutputDirectory) {
    $OutputDirectory = Join-Path $RepoRoot 'Content\Items\Armor\DarkmoonKnightSet'
}

$OutputDirectory = [IO.Path]::GetFullPath($OutputDirectory)
$baseDirectory = Join-Path $RepoRoot 'Content\Items\Armor\BlackKnightSet'
$sourcePath = Join-Path $RepoRoot 'NPCs\UnusedSprites\GhostOfTheDarkmoonKnight.png'
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null

# Use the NPC's authored olive/gray palette, rather than a whole-image color overlay.
# Every arm cell and walk frame retains the Black Knight rig's exact opacity and alignment.
$bodyPalette = @{
    '0C0B0C' = '1E1E14'; '15121D' = '323124'; '494B81' = '676455'
    '5C5F9B' = '8E8B7D'; '152433' = '323124'; '253E63' = '676455'
    '1C2D48' = '4E4C3D'; '606690' = 'A4A194'; '161E35' = '3C392C'
    '7E81AF' = 'B4B2A7'; '060A13' = '1E1E14'; '222222' = '323124'
}
$legsPalette = @{
    '070614' = '1E1E14'; '0E122C' = '323124'; '131939' = '4E4C3D'
    '141741' = '676455'; '1C2A45' = '6C695A'; '2A3758' = '8E8B7D'
    '34495E' = 'A4A194'
}

function Write-RecoloredSheet {
    param([string]$Source, [string]$Destination, [hashtable]$Palette)

    $bitmap = [Drawing.Bitmap]::FromFile($Source)
    try {
        for ($y = 0; $y -lt $bitmap.Height; $y++) {
            for ($x = 0; $x -lt $bitmap.Width; $x++) {
                $color = $bitmap.GetPixel($x, $y)
                if ($color.A -eq 0) {
                    continue
                }

                $key = '{0:X2}{1:X2}{2:X2}' -f $color.R, $color.G, $color.B
                if (-not $Palette.ContainsKey($key)) {
                    throw "Unmapped source color $key in $Source"
                }

                $replacement = [Drawing.ColorTranslator]::FromHtml('#' + $Palette[$key])
                $replacement = [Drawing.Color]::FromArgb($color.A, $replacement)
                $bitmap.SetPixel($x, $y, $replacement)
            }
        }

        $bitmap.Save($Destination, [Drawing.Imaging.ImageFormat]::Png)
    }
    finally {
        $bitmap.Dispose()
    }
}

function Write-ItemIcon {
    param([string]$SheetPath, [string]$IconPath)

    $sheet = [Drawing.Bitmap]::FromFile($SheetPath)
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
            $icon.Save($IconPath, [Drawing.Imaging.ImageFormat]::Png)
        }
        finally {
            $icon.Dispose()
        }
    }
    finally {
        $sheet.Dispose()
    }
}

Write-RecoloredSheet -Source (Join-Path $baseDirectory 'BlackKnightBreastplate_Body.png') -Destination (Join-Path $OutputDirectory 'DarkmoonKnightBreastplate_Body.png') -Palette $bodyPalette
Write-RecoloredSheet -Source (Join-Path $baseDirectory 'BlackKnightGreaves_Legs.png') -Destination (Join-Path $OutputDirectory 'DarkmoonKnightGreaves_Legs.png') -Palette $legsPalette

$npc = [Drawing.Bitmap]::FromFile($sourcePath)
$legacyGlow = [Drawing.Bitmap]::FromFile((Join-Path $RepoRoot 'NPCs\UnusedSprites\Ghost of the Darkmoon Knight Glow.png'))
$referenceHead = [Drawing.Bitmap]::FromFile((Join-Path $baseDirectory 'BlackKnightHelmet_Head.png'))
$head = [Drawing.Bitmap]::new(40, 1120, [Drawing.Imaging.PixelFormat]::Format32bppArgb)
$eyeGlow = [Drawing.Bitmap]::new(40, 1120, [Drawing.Imaging.PixelFormat]::Format32bppArgb)
try {
    if ($npc.Width -ne 70 -or $npc.Height -ne 896) {
        throw 'Expected the original 70x896 Darkmoon Knight NPC sheet.'
    }
    if ($legacyGlow.Width -ne $npc.Width -or $legacyGlow.Height -ne $npc.Height) {
        throw 'The archived eye glow must match the original NPC sheet.'
    }

    for ($frame = 0; $frame -lt 20; $frame++) {
        $frameTop = $frame * 56
        $referenceTop = 56
        for ($y = 0; $y -lt 56; $y++) {
            for ($x = 0; $x -lt 40; $x++) {
                if ($referenceHead.GetPixel($x, $frameTop + $y).A -gt 0) {
                    $referenceTop = [Math]::Min($referenceTop, $y)
                }
            }
        }

        # Lift the horned helmet, cyan eye and exposed blue face colors from frame 0.
        # Mirror to the player rig's right-facing orientation and preserve its per-frame head bob.
        for ($y = 2; $y -lt 28; $y++) {
            for ($x = 30; $x -lt 50; $x++) {
                # The lower right corner belongs to the NPC's pauldron, not its helmet.
                if ($y -ge 24 -and $x -ge 44) {
                    continue
                }

                $color = $npc.GetPixel($x, $y)
                if ($color.A -gt 0) {
                    # The ghost NPC bakes translucency into its PNG; equipment needs an opaque face.
                    $color = [Drawing.Color]::FromArgb(255, $color.R, $color.G, $color.B)
                    $head.SetPixel(10 + 49 - $x, $frameTop + $referenceTop + $y - 2, $color)
                }

                # Extract the original full-bright eyes with the identical helmet registration.
                # The source is left-facing; every puppet cell uses the same mirrored crop.
                $glowColor = $legacyGlow.GetPixel($x, $y)
                if ($glowColor.A -gt 0) {
                    # Legacy PostDraw applied the same glow twice. Bake that coverage into
                    # one mask so its original intensity survives with a single draw call.
                    $glowAlpha = [int][Math]::Round(255 * (1 - [Math]::Pow(1 - $glowColor.A / 255.0, 2)))
                    $glowColor = [Drawing.Color]::FromArgb($glowAlpha, $glowColor.R, $glowColor.G, $glowColor.B)
                    $eyeGlow.SetPixel(10 + 49 - $x, $frameTop + $referenceTop + $y - 2, $glowColor)
                }
            }
        }
    }

    $head.Save((Join-Path $OutputDirectory 'DarkmoonKnightHelmet_Head.png'), [Drawing.Imaging.ImageFormat]::Png)
    $eyeGlow.Save((Join-Path $OutputDirectory 'DarkmoonKnightHelmet_Head_Glow.png'), [Drawing.Imaging.ImageFormat]::Png)
}
finally {
    $npc.Dispose()
    $legacyGlow.Dispose()
    $eyeGlow.Dispose()
    $referenceHead.Dispose()
    $head.Dispose()
}

foreach ($part in @('Helmet', 'Breastplate', 'Greaves')) {
    $suffix = @{ Helmet = 'Head'; Breastplate = 'Body'; Greaves = 'Legs' }[$part]
    Write-ItemIcon -SheetPath (Join-Path $OutputDirectory "DarkmoonKnight${part}_${suffix}.png") -IconPath (Join-Path $OutputDirectory "DarkmoonKnight${part}.png")
}

Write-Output "Generated seven Darkmoon Knight PNGs (including the eye glow) in $OutputDirectory"
