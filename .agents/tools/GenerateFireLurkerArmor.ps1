<# Native-size Fire Lurker equipment conversion. Original head/leg pixels,
   source palette, hard 2x2 marks and proven composite-arm geometry. #>
[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$RepoRoot,[string]$OutputDirectory,[string]$AssetsDirectory)
$ErrorActionPreference='Stop';Add-Type -AssemblyName System.Drawing
if(!$OutputDirectory){$OutputDirectory=Join-Path $RepoRoot 'Content\Items\Armor\Enemy\FireLurkerSet'}
if(!$AssetsDirectory){$AssetsDirectory=Join-Path $RepoRoot '.agents\tools\assets'}
New-Item -ItemType Directory -Force $OutputDirectory|Out-Null
$npc=[Drawing.Bitmap]::FromFile((Join-Path $RepoRoot 'NPCs\Enemies\FireLurker.png'))
$arms=[Drawing.Bitmap]::FromFile((Join-Path $AssetsDirectory 'FireLurkerArmsTemplate.png'));$hands=[Drawing.Bitmap]::FromFile((Join-Path $AssetsDirectory 'FireLurkerHandsTemplate.png'))
if($npc.Width -ne 40 -or $npc.Height -ne 840){throw 'Expected native 40x840 Fire Lurker'}
foreach($b in @($arms,$hands)){if($b.Width -ne 360 -or $b.Height -ne 224){throw 'Invalid native arm template'}}
function C([string]$s){[Drawing.ColorTranslator]::FromHtml('#'+$s)}
function HexColor($c){'{0:X2}{1:X2}{2:X2}' -f $c.R,$c.G,$c.B}
function Block($b,[int]$x,[int]$y,$c){foreach($dy in 0,1){foreach($dx in 0,1){$b.SetPixel($x+$dx,$y+$dy,$c)}}}
function Blit($dest,$source,[int]$ox,[int]$oy){for($y=0;$y -lt $source.Height;$y+=2){for($x=0;$x -lt $source.Width;$x+=2){$c=$source.GetPixel($x,$y);if($c.A){Block $dest ($x+$ox) ($y+$oy) $c}}}}
$headPart=[Drawing.Bitmap]::new(40,56);$torso=[Drawing.Bitmap]::new(40,56);$frontCap=[Drawing.Bitmap]::new(40,56);$backCap=[Drawing.Bitmap]::new(40,56)
$head=[Drawing.Bitmap]::new(40,1120);$body=[Drawing.Bitmap]::new(360,224);$legs=[Drawing.Bitmap]::new(40,1120)
$ramp=@{'201C1C'='300400';'242131'='300400';'3D3336'='570505';'9C8F9B'='7C0C00';'D4C7D4'='BC1400';'DDD2DD'='E06C0C'}
try{
 # Face/jaw bounds exclude the separate shoulder starting at source x24.
 foreach($row in @(@(14,10,20),@(16,8,22),@(18,8,24),@(20,6,22),@(22,6,20),@(24,6,20),@(26,8,18),@(28,10,14))){$y=$row[0];for($x=$row[1];$x -le $row[2];$x+=2){$c=$npc.GetPixel($x,$y);if($c.A){Block $headPart (38-$x) $y $c}}}
 # Fire-blackened chest reconstruction, without pale source knuckles.
 $rows=@{
  28=@('300400','710707','941800','BC1400','941800','300400','300400','300400')
  30=@('300400','710707','941800','E06C0C','710707','300400','570505','300400')
  32=@('300400','570505','7C0C00','BC1400','7C0C00','300400','570505','300400')
  34=@('300400','941800','BC1400','941800','710707','941800','941800','300400')
  36=@('300400','941800','BC1400','BC1400','E06C0C','300400','300400','300400')
 }
 foreach($y in 28,30,32,34,36){for($i=0;$i -lt 8;$i++){Block $torso (14+$i*2) $y (C $rows[$y][$i])}}
 # Original high shoulder mass belongs to caps, not the rotating arms.
 foreach($row in @(@(20,24,28),@(22,22,30),@(24,22,30),@(26,24,30),@(28,24,30))){$y=$row[0];for($x=$row[1];$x -le $row[2];$x+=2){$c=$npc.GetPixel($x,$y);if($c.A){Block $frontCap (38-$x) $y $c}}}
 foreach($p in @(@(26,26,'300400'),@(28,26,'570505'),@(26,28,'941800'),@(28,28,'BC1400'),@(26,30,'570505'),@(28,30,'300400'))){Block $backCap $p[0] $p[1] (C $p[2])}
 . (Join-Path $PSScriptRoot 'PaintFireLurkerArms.ps1')
 $armPoses=Paint-FireLurkerArms $body $arms $hands (Join-Path $AssetsDirectory 'FireLurkerForearmTemplate.png')
 $armPoses|ConvertTo-Json -Depth 5|Set-Content (Join-Path $OutputDirectory 'FireLurkerArmPoses.json')
 foreach($r in 0,2){foreach($c in 0,1){Blit $body $torso ($c*40) ($r*56)};Blit $body $frontCap 0 (($r+1)*56);Blit $body $backCap 40 (($r+1)*56)}
 # Native locomotion: source 0 is standing, the running cycle loops 2..14.
 # There are 13 walk cells, repeated once to populate the 14 player phases.
 for($f=0;$f -lt 20;$f++){$bob=if($f -in @(7,8,9,14,15,16)){-2}else{0};Blit $head $headPart 0 ($f*56+$bob)
  $nf=if($f -eq 5){1}elseif($f -ge 6){2+[int][Math]::Floor(($f-6)*13/14)}else{0}
  for($y=40;$y -lt 56;$y+=2){for($x=0;$x -lt 40;$x+=2){$c=$npc.GetPixel($x,$nf*56+$y);if($c.A){Block $legs (38-$x) ($f*56+$y) $c}}}
  $shadow=@('300400','570505','7C0C00','941800','710707','570505','570505','300400')
  $hip=@('300400','570505','7C0C00','BC1400','D82400','941800','7C0C00','300400')
  foreach($y in 34,36,38){$paint=if($y -eq 38){$hip}else{$shadow};for($i=0;$i -lt 8;$i++){Block $legs (14+2*$i) ($f*56+$y) (C $paint[$i])}}
 }
 $head.Save((Join-Path $OutputDirectory 'FireLurkerHeadpiece_Head.png'));$body.Save((Join-Path $OutputDirectory 'FireLurkerHide_Body.png'));$legs.Save((Join-Path $OutputDirectory 'FireLurkerLegs_Legs.png'))
}finally{foreach($b in @($npc,$arms,$hands,$headPart,$torso,$frontCap,$backCap,$head,$body,$legs)){$b.Dispose()}}
function Icon([string]$name,[string]$suffix){$sheet=[Drawing.Bitmap]::FromFile((Join-Path $OutputDirectory "${name}_${suffix}.png"));$b=[Drawing.Bitmap]::new(40,56)
 if($suffix -eq 'Body'){$g=[Drawing.Graphics]::FromImage($b);foreach($cell in @(@(1,1),@(0,0),@(0,1))){$g.DrawImage($sheet,[Drawing.Rectangle]::new(0,0,40,56),$cell[0]*40,$cell[1]*56,40,56,[Drawing.GraphicsUnit]::Pixel)};$g.Dispose()}else{for($y=0;$y -lt 56;$y+=2){for($x=0;$x -lt 40;$x+=2){$c=$sheet.GetPixel($x,$y);if($c.A){Block $b $x $y $c}}}}
 $l=40;$t=56;$r=0;$bottom=0;for($y=0;$y -lt 56;$y+=2){for($x=0;$x -lt 40;$x+=2){if($b.GetPixel($x,$y).A){$l=[Math]::Min($l,$x);$t=[Math]::Min($t,$y);$r=[Math]::Max($r,$x+2);$bottom=[Math]::Max($bottom,$y+2)}}}
 $icon=$b.Clone([Drawing.Rectangle]::new($l,$t,$r-$l,$bottom-$t),[Drawing.Imaging.PixelFormat]::Format32bppArgb);$icon.Save((Join-Path $OutputDirectory "$name.png"));Write-Output "$name icon $($r-$l)x$($bottom-$t)";foreach($o in @($sheet,$b,$icon)){$o.Dispose()}
}
Icon FireLurkerHeadpiece Head;Icon FireLurkerHide Body;Icon FireLurkerLegs Legs

