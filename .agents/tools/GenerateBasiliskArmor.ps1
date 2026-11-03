<# Builds BasiliskSet without resizing the NPC's mouth or leg artwork. #>
[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$RepoRoot,[string]$OutputDirectory,[string]$ArmsTemplatePath,[string]$HandsTemplatePath)
$ErrorActionPreference='Stop';Add-Type -AssemblyName System.Drawing
if(!$OutputDirectory){$OutputDirectory=Join-Path $RepoRoot 'Content\Items\Armor\Enemy\BasiliskSet'}
if(!$ArmsTemplatePath){$ArmsTemplatePath=Join-Path $RepoRoot '.agents\tools\assets\BasiliskArmsTemplate.png'}
if(!$HandsTemplatePath){$HandsTemplatePath=Join-Path $RepoRoot '.agents\tools\assets\BasiliskHandsTemplate.png'}
New-Item -ItemType Directory -Force $OutputDirectory | Out-Null
$npc=[Drawing.Bitmap]::FromFile((Join-Path $RepoRoot 'NPCs\Enemies\Basilisk\BasiliskWalker.png'))
$arms=[Drawing.Bitmap]::FromFile($ArmsTemplatePath);$hands=[Drawing.Bitmap]::FromFile($HandsTemplatePath)
$reference=[Drawing.Bitmap]::FromFile((Join-Path $RepoRoot 'Content\Items\Armor\Enemy\BlackKnightSet\BlackKnightHelmet_Head.png'))
if($npc.Width -ne 60 -or $npc.Height -ne 720){throw 'Expected original 60x720 Basilisk Walker sheet'}
foreach($b in @($arms,$hands)){if($b.Width -ne 360 -or $b.Height -ne 224){throw 'Expected 360x224 arm and hand templates'}}
function ColorValue([string]$s){[Drawing.ColorTranslator]::FromHtml('#'+$s)}
function HexColor($c){'{0:X2}{1:X2}{2:X2}' -f $c.R,$c.G,$c.B}
function Block($b,[int]$x,[int]$y,$c){foreach($dy in 0,1){foreach($dx in 0,1){$b.SetPixel($x+$dx,$y+$dy,$c)}}}
$metal=@{'201C1C'='2D251D';'242131'='262637';'3D3336'='3C3C56';'9C8F9B'='484966';'D4C7D4'='686894';'DDD2DD'='9890E1'}
$glove=@{'201C1C'='262637';'242131'='262637';'3D3336'='273068';'9C8F9B'='263981';'D4C7D4'='0020B4';'DDD2DD'='0020B4'}
$head=[Drawing.Bitmap]::new(40,1120);$animated=[Drawing.Bitmap]::new(768,1120);$body=[Drawing.Bitmap]::new(360,224);$legs=[Drawing.Bitmap]::new(40,1120)
$torso=[Drawing.Bitmap]::new(40,56);$frontCap=[Drawing.Bitmap]::new(40,56);$backCap=[Drawing.Bitmap]::new(40,56)
$headParts=@();$handMask=[bool[,]]::new(360,224)
try {
 # Keep the moving jaws intact, including the fully open silhouette.
 # Normalize the original NPC walk bob at the back skull rim, then let each
 # player body frame supply the normal Terraria head bob independently.
 for($phase=0;$phase -lt 12;$phase++){
  $part=[Drawing.Bitmap]::new(64,56);$rim=60
  for($y=0;$y -lt 22;$y+=2){if($npc.GetPixel(44,$phase*60+$y).A){$rim=[Math]::Min($rim,$y)}}
  $align=16-$rim
  for($y=0;$y -le 28;$y+=2){$sy=$y-$align;if($sy -lt 0){continue};for($x=0;$x -lt 60;$x+=2){$c=$npc.GetPixel($x,$phase*60+$sy);if($c.A){Block $part (68-$x) ($y-4) $c}}}
  $headParts+=,$part
 }
 $hg=[Drawing.Graphics]::FromImage($head);$ag=[Drawing.Graphics]::FromImage($animated);$idleTop=56
 for($f=0;$f -lt 20;$f++){
  $top=56;for($y=0;$y -lt 56;$y+=2){for($x=0;$x -lt 40;$x+=2){if($reference.GetPixel($x,$f*56+$y).A){$top=[Math]::Min($top,$y)}}};if($f -eq 0){$idleTop=$top};$bob=$top-$idleTop
  for($phase=0;$phase -lt 12;$phase++){$ag.DrawImageUnscaled($headParts[$phase],$phase*64,$f*56+$bob)}
  $hg.DrawImage($headParts[0],[Drawing.Rectangle]::new(0,$f*56+$bob,40,56),12,0,40,56,[Drawing.GraphicsUnit]::Pixel)
  $nf=if($f -ge 6){1+[int][Math]::Floor(($f-6)*11/14)}else{0}
  for($y=44;$y -lt 60;$y+=2){for($x=20;$x -le 52;$x+=2){$c=$npc.GetPixel($x,$nf*60+$y);if($c.A){Block $legs (56-$x) ($f*56+$y-4) $c}}}
 };$hg.Dispose();$ag.Dispose()
 # Original blue collar and central plate. Forearm regions are deliberately
 # excluded from the fixed torso, so moving hands never reveal an extra fist.
 for($y=30;$y -le 44;$y+=2){for($x=28;$x -le 40;$x+=2){$c=$npc.GetPixel($x,$y);if($c.A){Block $torso (56-$x) ($y-4) $c}}}
 # Seal the hidden back of the chest in source plate shades rather than
 # borrowing a source hand or arm outline into that stationary region.
 foreach($y in 30,32,34,36,38){Block $torso 28 $y (ColorValue '262637')}
 # Compact shoulder caps fitted to the native joint, using source metal and blue trim.
 $capRows=@{
  26=@('2D251D','263981','263981','2D251D')
  28=@('2D251D','9890E1','686894','262637')
  30=@('262637','686894','484966','2D251D')
 }
 foreach($y in 26,28,30){for($i=0;$i -lt 4;$i++){Block $frontCap (10+2*$i) $y (ColorValue $capRows[$y][$i])}}
 foreach($p in @(@(26,28,'2D251D'),@(28,28,'263981'),@(26,30,'484966'),@(28,30,'686894'))){Block $backCap $p[0] $p[1] (ColorValue $p[2])}
 for($y=0;$y -lt 224;$y+=2){for($x=80;$x -lt 360;$x+=2){$c=$arms.GetPixel($x,$y);if(!$c.A){continue};$k=HexColor $c;$hand=$hands.GetPixel($x,$y).A -gt 0
  if(!$hand -and $k -in @('201C1C','242131')){foreach($dy in -2,0,2){foreach($dx in -2,0,2){$nx=$x+$dx;$ny=$y+$dy;if($nx -ge [int][Math]::Floor($x/40)*40 -and $nx -lt ([int][Math]::Floor($x/40)+1)*40 -and $ny -ge [int][Math]::Floor($y/56)*56 -and $ny -lt ([int][Math]::Floor($y/56)+1)*56 -and $hands.GetPixel($nx,$ny).A){$hand=$true}}}}
  $handMask[$x,$y]=$hand;$palette=if($hand){$glove}else{$metal};if(!$palette.ContainsKey($k)){throw "Unknown arm shade: $k"};Block $body $x $y (ColorValue $palette[$k])
 }}
 for($y=0;$y -lt 224;$y+=2){for($x=80;$x -lt 360;$x+=2){if(!$handMask[$x,$y]){continue};$edge=$false;foreach($d in @(@(-2,0),@(2,0),@(0,-2),@(0,2))){$nx=$x+$d[0];$ny=$y+$d[1];if($nx -lt 80 -or $nx -ge 360 -or $ny -lt 0 -or $ny -ge 224 -or [int][Math]::Floor($nx/40) -ne [int][Math]::Floor($x/40) -or [int][Math]::Floor($ny/56) -ne [int][Math]::Floor($y/56) -or !$handMask[$nx,$ny]){$edge=$true}};if($edge){Block $body $x $y (ColorValue '262637')}}}
 $g=[Drawing.Graphics]::FromImage($body);foreach($row in 0,2){foreach($col in 0,1){$g.DrawImageUnscaled($torso,$col*40,$row*56)};$g.DrawImageUnscaled($frontCap,0,($row+1)*56);$g.DrawImageUnscaled($backCap,40,($row+1)*56)};$g.Dispose()
 $head.Save((Join-Path $OutputDirectory 'BasiliskHeadpiece_Head.png'));$animated.Save((Join-Path $OutputDirectory 'BasiliskHeadpiece_Head_Animated.png'));$body.Save((Join-Path $OutputDirectory 'BasiliskCarapace_Body.png'));$legs.Save((Join-Path $OutputDirectory 'BasiliskGreaves_Legs.png'))
 # Animate the inventory icon too, with a common crop across all 12 jaws.
 $l=64;$t=56;$r=0;$bottom=0;foreach($part in $headParts){for($y=0;$y -lt 56;$y+=2){for($x=0;$x -lt 64;$x+=2){if($part.GetPixel($x,$y).A){$l=[Math]::Min($l,$x);$t=[Math]::Min($t,$y);$r=[Math]::Max($r,$x+2);$bottom=[Math]::Max($bottom,$y+2)}}}}
 $w=$r-$l;$h=$bottom-$t;$icon=[Drawing.Bitmap]::new($w,$h*12);$g=[Drawing.Graphics]::FromImage($icon);for($phase=0;$phase -lt 12;$phase++){$g.DrawImage($headParts[$phase],[Drawing.Rectangle]::new(0,$phase*$h,$w,$h),$l,$t,$w,$h,[Drawing.GraphicsUnit]::Pixel)};$g.Dispose();$icon.Save((Join-Path $OutputDirectory 'BasiliskHeadpiece.png'));$icon.Dispose();Write-Output "Head icon: ${w}x${h}, 12 frames"
}
finally{foreach($b in @($npc,$arms,$hands,$reference,$head,$animated,$body,$legs,$torso,$frontCap,$backCap)+$headParts){$b.Dispose()}}
foreach($entry in @(@('BasiliskCarapace','Body'),@('BasiliskGreaves','Legs'))){$name=$entry[0];$suffix=$entry[1];$sheet=[Drawing.Bitmap]::FromFile((Join-Path $OutputDirectory "${name}_${suffix}.png"));$b=[Drawing.Bitmap]::new(40,56);$g=[Drawing.Graphics]::FromImage($b)
 if($suffix -eq 'Body'){foreach($cell in @(@(1,1),@(0,0),@(0,1))){$g.DrawImage($sheet,[Drawing.Rectangle]::new(0,0,40,56),$cell[0]*40,$cell[1]*56,40,56,[Drawing.GraphicsUnit]::Pixel)}}else{$g.DrawImage($sheet,[Drawing.Rectangle]::new(0,0,40,56),0,0,40,56,[Drawing.GraphicsUnit]::Pixel)};$g.Dispose();$sheet.Dispose()
 $l=40;$t=56;$r=0;$bottom=0;for($y=0;$y -lt 56;$y++){for($x=0;$x -lt 40;$x++){if($b.GetPixel($x,$y).A){$l=[Math]::Min($l,$x);$t=[Math]::Min($t,$y);$r=[Math]::Max($r,$x+1);$bottom=[Math]::Max($bottom,$y+1)}}};$icon=$b.Clone([Drawing.Rectangle]::new($l,$t,$r-$l,$bottom-$t),[Drawing.Imaging.PixelFormat]::Format32bppArgb);$icon.Save((Join-Path $OutputDirectory "$name.png"));Write-Output "$name icon: $($r-$l)x$($bottom-$t)";$icon.Dispose();$b.Dispose()
}
# Keep regenerated arms consistent with the Fire Lurker claw transplant.
$bodyPath=Join-Path $OutputDirectory 'BasiliskCarapace_Body.png'
& (Join-Path $PSScriptRoot 'TransferFireLurkerArmsToBasilisk.ps1') -FireBodyPath (Join-Path $RepoRoot 'Content\Items\Armor\Enemy\FireLurkerSet\FireLurkerHide_Body.png') -BasiliskBodyPath $bodyPath -OutputPath $bodyPath
