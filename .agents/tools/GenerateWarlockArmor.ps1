<#
.SYNOPSIS
Converts the Warlock NPC to standard head/body/leg equipment sheets.
.DESCRIPTION
Preserves the NPC's silver hair, face, beard and all authored leg poses.
Purple sleeve and bare-hand pixels use an independent working arm template.
#>
[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$RepoRoot,[string]$OutputDirectory,[string]$ArmsTemplatePath,[string]$HandsTemplatePath)
$ErrorActionPreference='Stop';Add-Type -AssemblyName System.Drawing
if(!$OutputDirectory){$OutputDirectory=Join-Path $RepoRoot 'Content\Items\Armor\WarlockSet'}
if(!$ArmsTemplatePath){$ArmsTemplatePath=Join-Path $RepoRoot '.agents\tools\assets\WarlockArmsTemplate.png'}
if(!$HandsTemplatePath){$HandsTemplatePath=Join-Path $RepoRoot '.agents\tools\assets\WarlockHandsTemplate.png'}
New-Item -ItemType Directory -Force $OutputDirectory | Out-Null
$npc=[Drawing.Bitmap]::FromFile((Join-Path $RepoRoot 'NPCs\Enemies\Warlock.png'))
$arms=[Drawing.Bitmap]::FromFile($ArmsTemplatePath);$hands=[Drawing.Bitmap]::FromFile($HandsTemplatePath)
$reference=[Drawing.Bitmap]::FromFile((Join-Path $RepoRoot 'Content\Items\Armor\BlackKnightSet\BlackKnightHelmet_Head.png'))
if($npc.Width -ne 46 -or $npc.Height -ne 840){throw 'Expected original 46x840 Warlock sheet'}
foreach($b in @($arms,$hands)){if($b.Width -ne 360 -or $b.Height -ne 224){throw 'Expected a 360x224 arm or hand-position template'}}
function ColorValue([string]$Hex){[Drawing.ColorTranslator]::FromHtml('#'+$Hex)}
function HexColor($c){'{0:X2}{1:X2}{2:X2}' -f $c.R,$c.G,$c.B}
function Block($b,[int]$x,[int]$y,$c){foreach($dy in 0,1){foreach($dx in 0,1){$b.SetPixel($x+$dx,$y+$dy,$c)}}}
$hair=@('363434','DEDBDE','A5A6A5','EFEBEF','414141')
$face=@('321812','FFBE84','4A4035','F1F1F1','FFAA73')
$cloth=@('0C0C0C','210039','310052','4A0073','523063','6B3C7B','7B4994','844D9C','292829')
$sleeve=@{'201C1C'='0C0C0C';'242131'='0C0C0C';'3D3336'='210039';'9C8F9B'='4A0073';'D4C7D4'='523063';'DDD2DD'='7B4994'}
$skin=@{'201C1C'='321812';'242131'='321812';'3D3336'='BE5D43';'9C8F9B'='BE5D43';'D4C7D4'='FF7D5A';'DDD2DD'='FF7D5A'}
$headPart=[Drawing.Bitmap]::new(40,56);$torso=[Drawing.Bitmap]::new(40,56)
$frontCap=[Drawing.Bitmap]::new(40,56);$backCap=[Drawing.Bitmap]::new(40,56)
$head=[Drawing.Bitmap]::new(40,1120);$body=[Drawing.Bitmap]::new(360,224);$legs=[Drawing.Bitmap]::new(40,1120)
$handMask=[bool[,]]::new(360,224)
try {
 # Hair and beard are one connected head layer. The much redder hands are
 # excluded; they must never remain baked into the head or fixed torso.
 for($y=10;$y -lt 40;$y+=2){for($x=12;$x -le 36;$x+=2){$c=$npc.GetPixel($x,6*56+$y);if(!$c.A){continue};$k=HexColor $c
  $facePixel=$y -ge 16 -and $y -le 24 -and $x -ge 14 -and $x -le 26 -and $k -in $face
  if($k -in $hair -or $facePixel -or ($x -eq 12 -and $y -eq 26)){Block $headPart (44-$x) $y $c}
 }}
 # Rebuild the occluded shirt as cloth only, without borrowing the source
 # fists or beard into this fixed layer. All shades are from the NPC robe.
 $rows=@{
  28=@('0C0C0C','210039','310052','4A0073','4A0073','210039','210039','0C0C0C')
  30=@('0C0C0C','210039','310052','4A0073','210039','210039','523063','0C0C0C')
  32=@('0C0C0C','210039','310052','4A0073','210039','210039','6B3C7B','0C0C0C')
  34=@('0C0C0C','210039','310052','4A0073','210039','4A0073','523063','0C0C0C')
  36=@('0C0C0C','210039','310052','4A0073','210039','310052','4A0073','0C0C0C')
  38=@('0C0C0C','210039','210039','210039','210039','4A0073','4A0073','0C0C0C')
 }
 foreach($y in 28,30,32,34,36,38){for($i=0;$i -lt 8;$i++){Block $torso (14+2*$i) $y (ColorValue $rows[$y][$i])}}
 # The original shoulder is a purple sleeve cap. Keep it independent of
 # the swinging sleeve; no white hair, orange fist or duplicate pad here.
 for($y=24;$y -le 30;$y+=2){for($x=28;$x -le 36;$x+=2){$c=$npc.GetPixel($x,6*56+$y);if($c.A -and (HexColor $c) -in $cloth){Block $frontCap (44-$x) $y $c}}}
 Block $backCap 26 28 (ColorValue '0C0C0C');Block $backCap 28 28 (ColorValue '210039')
 Block $backCap 26 30 (ColorValue '310052');Block $backCap 28 30 (ColorValue '4A0073')
 for($y=0;$y -lt 224;$y+=2){for($x=80;$x -lt 360;$x+=2){$c=$arms.GetPixel($x,$y);if(!$c.A){continue};$k=HexColor $c
  $hand=$hands.GetPixel($x,$y).A -gt 0
  if(!$hand -and $k -in @('201C1C','242131')){foreach($dy in -2,0,2){foreach($dx in -2,0,2){$nx=$x+$dx;$ny=$y+$dy
   if($nx -ge [int][Math]::Floor($x/40)*40 -and $nx -lt ([int][Math]::Floor($x/40)+1)*40 -and $ny -ge [int][Math]::Floor($y/56)*56 -and $ny -lt ([int][Math]::Floor($y/56)+1)*56 -and $hands.GetPixel($nx,$ny).A){$hand=$true}
  }}}
  $handMask[$x,$y]=$hand;$palette=if($hand){$skin}else{$sleeve}
  if(!$palette.ContainsKey($k)){throw "Unmapped arm-template shade: $k"};Block $body $x $y (ColorValue $palette[$k])
 }}
 # Deliberate dark contour on each bare fist, including light template
 # boundary texels. The brown NPC outline follows the hand, never the chest.
 for($y=0;$y -lt 224;$y+=2){for($x=80;$x -lt 360;$x+=2){if(!$handMask[$x,$y]){continue};$edge=$false
  foreach($d in @(@(-2,0),@(2,0),@(0,-2),@(0,2))){$nx=$x+$d[0];$ny=$y+$d[1]
   if($nx -lt 80 -or $nx -ge 360 -or $ny -lt 0 -or $ny -ge 224 -or [int][Math]::Floor($nx/40) -ne [int][Math]::Floor($x/40) -or [int][Math]::Floor($ny/56) -ne [int][Math]::Floor($y/56) -or !$handMask[$nx,$ny]){$edge=$true}
  };if($edge){Block $body $x $y (ColorValue '321812')}
 }}
 $g=[Drawing.Graphics]::FromImage($body)
 foreach($row in 0,2){foreach($col in 0,1){$g.DrawImageUnscaled($torso,$col*40,$row*56)};$g.DrawImageUnscaled($frontCap,0,($row+1)*56);$g.DrawImageUnscaled($backCap,40,($row+1)*56)};$g.Dispose()
 $g=[Drawing.Graphics]::FromImage($head);$idleTop=56
 for($f=0;$f -lt 20;$f++){
  $top=56;for($y=0;$y -lt 56;$y+=2){for($x=0;$x -lt 40;$x+=2){if($reference.GetPixel($x,$f*56+$y).A){$top=[Math]::Min($top,$y)}}}
  if($f -eq 0){$idleTop=$top};$g.DrawImageUnscaled($headPart,0,$f*56+$top-$idleTop)
  $nf=if($f -eq 5){0}elseif($f -ge 6){$f-5}else{6}
  for($y=38;$y -lt 56;$y+=2){for($x=6;$x -le 38;$x+=2){$c=$npc.GetPixel($x,$nf*56+$y);if($c.A -and (HexColor $c) -in $cloth){Block $legs (44-$x) ($f*56+$y) $c}}}
 };$g.Dispose()
 $head.Save((Join-Path $OutputDirectory 'WarlockHeadpiece_Head.png'));$body.Save((Join-Path $OutputDirectory 'WarlockRobe_Body.png'));$legs.Save((Join-Path $OutputDirectory 'WarlockBoots_Legs.png'))
}
finally{foreach($b in @($npc,$arms,$hands,$reference,$headPart,$torso,$frontCap,$backCap,$head,$body,$legs)){$b.Dispose()}}
function Write-Icon([string]$Name,[string]$Suffix){
 $sheet=[Drawing.Bitmap]::FromFile((Join-Path $OutputDirectory "${Name}_${Suffix}.png"))
 if($Suffix -eq 'Body'){$b=[Drawing.Bitmap]::new(40,56);$g=[Drawing.Graphics]::FromImage($b);foreach($cell in @(@(1,1),@(0,0),@(0,1))){$g.DrawImage($sheet,[Drawing.Rectangle]::new(0,0,40,56),$cell[0]*40,$cell[1]*56,40,56,[Drawing.GraphicsUnit]::Pixel)};$g.Dispose();$sheet.Dispose();$sheet=$b}
 $left=40;$top=56;$right=0;$bottom=0
 for($y=0;$y -lt 56;$y++){for($x=0;$x -lt 40;$x++){if($sheet.GetPixel($x,$y).A){$left=[Math]::Min($left,$x);$top=[Math]::Min($top,$y);$right=[Math]::Max($right,$x+1);$bottom=[Math]::Max($bottom,$y+1)}}}
 $icon=$sheet.Clone([Drawing.Rectangle]::new($left,$top,$right-$left,$bottom-$top),[Drawing.Imaging.PixelFormat]::Format32bppArgb);$icon.Save((Join-Path $OutputDirectory "$Name.png"));$icon.Dispose();$sheet.Dispose();Write-Output "$Name icon: $($right-$left)x$($bottom-$top)"
}
Write-Icon WarlockHeadpiece Head;Write-Icon WarlockRobe Body;Write-Icon WarlockBoots Legs
