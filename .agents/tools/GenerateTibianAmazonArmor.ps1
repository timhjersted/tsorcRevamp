<#
.SYNOPSIS
Converts the original Tibian Amazon into standard player equipment sheets.
.DESCRIPTION
Keeps source hair/face, tunic palette and every authored leg pose. Uses a
snapshot of the finished Valkyrie arm masks/outlines; never modifies that set.
#>
[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$RepoRoot,[string]$OutputDirectory,[string]$ArmsTemplatePath,[string]$HandsTemplatePath,[string]$DetailTemplatePath)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
if(-not $OutputDirectory){$OutputDirectory=Join-Path $RepoRoot 'Content\Items\Armor\Enemy\TibianAmazonSet'}
if(-not $ArmsTemplatePath){$ArmsTemplatePath=Join-Path $RepoRoot '.agents\tools\assets\TibianAmazonArmsTemplate.png'}
if(-not $HandsTemplatePath){$HandsTemplatePath=Join-Path $RepoRoot '.agents\tools\assets\TibianAmazonHandsTemplate.png'}
if(-not $DetailTemplatePath){$DetailTemplatePath=Join-Path $RepoRoot '.agents\tools\assets\TibianAmazonDetailTemplate.png'}
$OutputDirectory=[IO.Path]::GetFullPath($OutputDirectory);New-Item -ItemType Directory -Force $OutputDirectory|Out-Null
$npc=[Drawing.Bitmap]::FromFile((Join-Path $RepoRoot 'NPCs\Enemies\TibianAmazon.png'))
$template=[Drawing.Bitmap]::FromFile($ArmsTemplatePath)
$hands=[Drawing.Bitmap]::FromFile($HandsTemplatePath)
$details=[Drawing.Bitmap]::FromFile($DetailTemplatePath)
if($details.Width -ne 360 -or $details.Height -ne 224){throw 'Expected a 360x224 authored shoulder/detail layer.'}
$gloveMask=[bool[,]]::new(360,224)
if($hands.Width -ne 360 -or $hands.Height -ne 224){throw 'Expected a 360x224 composite hand-position mask.'}
$referenceHead=[Drawing.Bitmap]::FromFile((Join-Path $RepoRoot 'Content\Items\Armor\Enemy\BlackKnightSet\BlackKnightHelmet_Head.png'))
if($npc.Width -ne 60 -or $npc.Height -ne 840){throw 'Expected the original 60x840 Amazon sheet.'}
function Block([Drawing.Bitmap]$Bitmap,[int]$X,[int]$Y,[Drawing.Color]$Color){foreach($dy in 0,1){foreach($dx in 0,1){$Bitmap.SetPixel($X+$dx,$Y+$dy,$Color)}}}
function Color([string]$Hex){return [Drawing.ColorTranslator]::FromHtml('#'+$Hex)}
function Hex([Drawing.Color]$Color){return '{0:X2}{1:X2}{2:X2}' -f $Color.R,$Color.G,$Color.B}
$hairColors=@('280E0A','3A190F','663620','DB8A40','EFAE60','FFD5A0','310E03')
$skinColors=@('3C1D16','C89868','A97E57','DDAD7F')
$greenColors=@('1B1B0F','2B3018','37401D','475732','6E7E64')
$pantColors=@('182018','3E4637','2F261F','565042','201710','3D3732','2A1E15')
$skinPalette=@{'201C1C'='3C1D16';'242131'='3C1D16';'3D3336'='A97E57';'9C8F9B'='A97E57';'D4C7D4'='C89868';'DDD2DD'='DDAD7F'}
$glovePalette=@{'201C1C'='1B1B0F';'242131'='1B1B0F';'3D3336'='2B3018';'9C8F9B'='475732';'D4C7D4'='6E7E64';'DDD2DD'='6E7E64'}
$headPart=[Drawing.Bitmap]::new(40,56);$torso=[Drawing.Bitmap]::new(40,56);$frontCap=[Drawing.Bitmap]::new(40,56);$backCap=[Drawing.Bitmap]::new(40,56)
$head=[Drawing.Bitmap]::new(40,1120);$legs=[Drawing.Bitmap]::new(40,1120);$body=[Drawing.Bitmap]::new(360,224)
try {
 # Hair strands remain on the head layer. Skin belongs to the face only here;
 # the source's bent forearm must not be baked into the fixed chest artwork.
 for($y=4;$y -lt 34;$y+=2){for($x=18;$x -lt 48;$x+=2){
  $c=$npc.GetPixel($x,6*56+$y);if(-not $c.A){continue};$k=Hex $c
  $face=$y -le 30 -and $x -ge 20 -and $x -le 32 -and ($k -in $skinColors -or $k -in @('FF4D00','F1F1F1'))
  if($k -in $hairColors -or $face){Block $headPart (50-$x) $y $c}
 }}
 # Only upper shirt samples are suitable for the fixed chest. The source's
 # bright green pixels at y32..36 include a foreground glove, not more shirt.
 for($y=28;$y -lt 32;$y+=2){for($x=24;$x -lt 40;$x+=2){
  $c=$npc.GetPixel($x,6*56+$y);$k=Hex $c
  if($k -notin $greenColors){$c=Color '475732'}elseif($k -eq '1B1B0F'){$c=Color '2B3018'}
  Block $torso (50-$x) $y $c
 }}
 # Clean cloth behind the moving forearms: taper toward the waist and omit
 # the glove silhouette that used to remain visible as a stationary extra hand.
 $shirtRows=@{
  32=@('2B3018','475732','475732','475732','475732','37401D','37401D','2B3018')
  34=@('2B3018','475732','475732','475732','37401D','37401D','2B3018')
 }
 foreach($y in 32,34){$start=if($y -eq 32){14}else{16};for($i=0;$i -lt $shirtRows[$y].Count;$i++){Block $torso ($start+2*$i) $y (Color $shirtRows[$y][$i])}}
 # The exposed source belly is C89868, bounded by 3C1D16 rather than green.
 # Walk frame 4 exposes one more central skin block than the idle glove does.
 foreach($x in 18,28){Block $torso $x 36 (Color '3C1D16')}
 foreach($x in 20,22,24,26){Block $torso $x 36 (Color 'C89868')}
 # Preserve the complete original waist, including every edge block.
 for($x=22;$x -lt 36;$x+=2){Block $torso (50-$x) 38 ($npc.GetPixel($x,6*56+38))}
 for($y=24;$y -lt 32;$y+=2){for($x=34;$x -lt 42;$x+=2){$c=$npc.GetPixel($x,6*56+$y);if($c.A -and (Hex $c) -in $greenColors){Block $frontCap (50-$x) $y $c}}}
 Block $backCap 26 28 (Color '2B3018');Block $backCap 28 28 (Color '475732')
 Block $backCap 26 30 (Color '475732');Block $backCap 28 30 (Color '6E7E64')
 # Bare arms and green gloves in every normal/use/composite pose. A native
 # Terraria hand mask locates the distal end even when an arm is raised or
 # folded; shoulder-distance tests incorrectly invert sleeves and gloves.
 for($y=0;$y -lt 224;$y+=2){for($x=80;$x -lt 360;$x+=2){
  $c=$template.GetPixel($x,$y);if(-not $c.A){continue};$k=Hex $c
  $glove=$hands.GetPixel($x,$y).A -gt 0
  # Include the armor template's adjacent contour around the true hand, so
  # glove edges become dark green rather than retaining brown skin outlines.
  if(-not $glove -and $k -in @('201C1C','242131')){
   foreach($dy in -2,0,2){foreach($dx in -2,0,2){$nx=$x+$dx;$ny=$y+$dy
    if($nx -ge [int][Math]::Floor($x/40)*40 -and $nx -lt ([int][Math]::Floor($x/40)+1)*40 -and $ny -ge [int][Math]::Floor($y/56)*56 -and $ny -lt ([int][Math]::Floor($y/56)+1)*56 -and $hands.GetPixel($nx,$ny).A){$glove=$true}
   }}
  }
  $palette=if($glove){$glovePalette}else{$skinPalette}
  $gloveMask[$x,$y]=$glove
  if(-not $palette.ContainsKey($k)){throw "Unknown arm template color: $k"}
  Block $body $x $y (Color $palette[$k])
 }}
 # The native hand mask contains light edge pixels as well as dark ones.
 # Outline the actual glove silhouette; palette substitution alone leaves
 # those light outer edges unoutlined in walking and folded swing poses.
 for($y=0;$y -lt 224;$y+=2){for($x=80;$x -lt 360;$x+=2){if(-not $gloveMask[$x,$y]){continue}
  $edge=$false
  foreach($d in @(@(-2,0),@(2,0),@(0,-2),@(0,2))){$nx=$x+$d[0];$ny=$y+$d[1]
   if($nx -lt 80 -or $nx -ge 360 -or $ny -lt 0 -or $ny -ge 224 -or [int][Math]::Floor($nx/40) -ne [int][Math]::Floor($x/40) -or [int][Math]::Floor($ny/56) -ne [int][Math]::Floor($y/56) -or -not $gloveMask[$nx,$ny]){$edge=$true}
  }
  if($edge){Block $body $x $y (Color '1B1B0F')}
 }}
 $bg=[Drawing.Graphics]::FromImage($body)
 foreach($r in @(0,2)){
  foreach($col in @(0,1)){$bg.DrawImageUnscaled($torso,$col*40,$r*56)}
  $bg.DrawImageUnscaled($frontCap,0,($r+1)*56);$bg.DrawImageUnscaled($backCap,40,($r+1)*56)
 };$bg.Dispose()
 # Preserve the user's authored green shoulder pads and arm/hand touchups.
 # Full front-cap cells are replaced so their exact expanded shape survives.
 foreach($row in 1,3){for($y=0;$y -lt 56;$y++){for($x=0;$x -lt 40;$x++){$body.SetPixel($x,$row*56+$y,$details.GetPixel($x,$row*56+$y))}}}
 for($y=0;$y -lt 224;$y+=2){for($x=80;$x -lt 360;$x+=2){$c=$details.GetPixel($x,$y);if($c.A){Block $body $x $y $c}}}
 $hg=[Drawing.Graphics]::FromImage($head);$idleTop=56
 for($f=0;$f -lt 20;$f++){
  $top=56;for($y=0;$y -lt 56;$y+=2){for($x=0;$x -lt 40;$x+=2){if($referenceHead.GetPixel($x,$f*56+$y).A){$top=[Math]::Min($top,$y)}}}
  if($f -eq 0){$idleTop=$top};$hg.DrawImageUnscaled($headPart,0,$f*56+$top-$idleTop)
  # Skeleton walk phases match player frames 6..19; 0 is jump, 6 idle.
  $nf=if($f -eq 5){0}elseif($f -ge 6){$f-5}else{6}
  for($y=36;$y -lt 56;$y+=2){for($x=12;$x -lt 50;$x+=2){
   $c=$npc.GetPixel($x,$nf*56+$y);if(-not $c.A){continue};$k=Hex $c
   if($k -in $pantColors -or ($y -ge 40 -and $k -in $skinColors)){Block $legs (50-$x) ($f*56+$y) $c}
  }}
 };$hg.Dispose()
 $head.Save((Join-Path $OutputDirectory 'TibianAmazonHeadpiece_Head.png'))
 $body.Save((Join-Path $OutputDirectory 'TibianAmazonTunic_Body.png'))
 $legs.Save((Join-Path $OutputDirectory 'TibianAmazonBoots_Legs.png'))
}
finally{foreach($b in @($npc,$template,$hands,$details,$referenceHead,$headPart,$torso,$frontCap,$backCap,$head,$body,$legs)){$b.Dispose()}}

function Write-Icon([string]$Name,[string]$Suffix){
 $sheet=[Drawing.Bitmap]::FromFile((Join-Path $OutputDirectory "${Name}_${Suffix}.png"))
 if($Suffix -eq 'Body'){$b=[Drawing.Bitmap]::new(40,56);$g=[Drawing.Graphics]::FromImage($b);foreach($cell in @(@(1,1),@(0,0),@(0,1))){$g.DrawImage($sheet,[Drawing.Rectangle]::new(0,0,40,56),$cell[0]*40,$cell[1]*56,40,56,[Drawing.GraphicsUnit]::Pixel)};$g.Dispose();$sheet.Dispose();$sheet=$b}
 $left=40;$top=56;$right=0;$bottom=0
 for($y=0;$y -lt 56;$y++){for($x=0;$x -lt 40;$x++){if($sheet.GetPixel($x,$y).A){$left=[Math]::Min($left,$x);$top=[Math]::Min($top,$y);$right=[Math]::Max($right,$x+1);$bottom=[Math]::Max($bottom,$y+1)}}}
 $icon=$sheet.Clone([Drawing.Rectangle]::new($left,$top,$right-$left,$bottom-$top),[Drawing.Imaging.PixelFormat]::Format32bppArgb)
 $icon.Save((Join-Path $OutputDirectory "$Name.png"));$icon.Dispose();$sheet.Dispose();Write-Output "$Name inventory icon: $($right-$left)x$($bottom-$top)"
}
Write-Icon TibianAmazonHeadpiece Head
Write-Icon TibianAmazonTunic Body
Write-Icon TibianAmazonBoots Legs

