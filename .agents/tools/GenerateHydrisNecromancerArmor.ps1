<# Converts the Hydris Necromancer to native Terraria equipment sheets.
   Cleans source art into a compact palette with hard alpha and
   2x2 blocks throughout. Waist motion stays in the leg sheet. #>
[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$RepoRoot,[string]$OutputDirectory,[string]$AssetsDirectory,[string]$NpcSourcePath,[switch]$LegsOnly)
$ErrorActionPreference='Stop';Add-Type -AssemblyName System.Drawing
if(!$OutputDirectory){$OutputDirectory=Join-Path $RepoRoot 'Content\Items\Armor\Enemy\HydrisNecromancerSet'}
if(!$AssetsDirectory){$AssetsDirectory=Join-Path $RepoRoot '.agents\tools\assets'}
New-Item -ItemType Directory -Force $OutputDirectory|Out-Null
if(!$NpcSourcePath){$NpcSourcePath=Join-Path $RepoRoot 'NPCs\Enemies\SuperHardMode\HydrisNecromancer.png'}
$npc=[Drawing.Bitmap]::FromFile($NpcSourcePath)
$arms=[Drawing.Bitmap]::FromFile((Join-Path $AssetsDirectory 'HydrisNecromancerArmsTemplate.png'))
$hands=[Drawing.Bitmap]::FromFile((Join-Path $AssetsDirectory 'HydrisNecromancerHandsTemplate.png'))
$reference=[Drawing.Bitmap]::FromFile((Join-Path $RepoRoot 'Content\Items\Armor\Enemy\BlackKnightSet\BlackKnightHelmet_Head.png'))
if($npc.Width -ne 46 -or $npc.Height -ne 840){throw 'Expected 46x840, 15-frame NPC'}
foreach($b in @($arms,$hands)){if($b.Width -ne 360 -or $b.Height -ne 224){throw 'Invalid arm template'}}
function ColorValue([string]$s){[Drawing.ColorTranslator]::FromHtml('#'+$s)}
function HexColor($c){'{0:X2}{1:X2}{2:X2}' -f $c.R,$c.G,$c.B}
function Block($b,[int]$x,[int]$y,$c){foreach($dy in 0,1){foreach($dx in 0,1){$b.SetPixel($x+$dx,$y+$dy,$c)}}}
$palette=@('000000','05052D','0D1750','2A306A','424579','62628A','8887A0','AAA7AA','C0CFDF','1E0700','AA4D3C','E56D50','FF7D5A','18144D','282062','4F6DA3','97B5DF','081827','485B75','7095C4')
$colors=@($palette|ForEach-Object{ColorValue $_});$sourceCache=@{}
function SourceColor([int]$x,[int]$y){
 $key="$x,$y";if($sourceCache.ContainsKey($key)){return $sourceCache[$key]}
 $rr=0.0;$gg=0.0;$bb=0.0;$weight=0
 foreach($dy in 0,1){foreach($dx in 0,1){$p=$npc.GetPixel($x+$dx,$y+$dy);$weight+=$p.A;$rr+=$p.R*$p.A;$gg+=$p.G*$p.A;$bb+=$p.B*$p.A}}
 if($weight -lt 384){$value=[Drawing.Color]::Transparent}else{
  $rr/=$weight;$gg/=$weight;$bb/=$weight;$best=0;$distance=[double]::MaxValue
  for($i=0;$i -lt $colors.Count;$i++){$p=$colors[$i];$d=($rr-$p.R)*($rr-$p.R)+($gg-$p.G)*($gg-$p.G)+($bb-$p.B)*($bb-$p.B);if($d -lt $distance){$distance=$d;$best=$i}}
  $value=$colors[$best]
 };$sourceCache[$key]=$value;return $value
}
$headPart=[Drawing.Bitmap]::new(40,56);$torso=[Drawing.Bitmap]::new(40,56)
$frontCap=[Drawing.Bitmap]::new(40,56);$backCap=[Drawing.Bitmap]::new(40,56)
$head=[Drawing.Bitmap]::new(40,1120);$body=[Drawing.Bitmap]::new(360,224);$legs=[Drawing.Bitmap]::new(40,1120)
$sleeve=@{'201C1C'='000000';'242131'='000000';'3D3336'='05052D';'9C8F9B'='0D1750';'D4C7D4'='2A306A';'DDD2DD'='424579'}
$glove=@{'201C1C'='081827';'242131'='081827';'3D3336'='4F6DA3';'9C8F9B'='7095C4';'D4C7D4'='97B5DF';'DDD2DD'='BDD0E0'}
try {
 # All source helmet/face pixels above the collar, including the rear crown
 # points and chin. Source faces left; equipment faces right.
 for($y=8;$y -lt 30;$y+=2){for($x=10;$x -lt 42;$x+=2){$c=SourceColor $x (336+$y);if($c.A){Block $headPart (44-$x) $y $c}}}
 # Consolidate the crown's tiny mixed shades into clear blue/silver ramps.
 $crownRamp=@{'485B75'='424579';'081827'='05052D';'4F6DA3'='2A306A';'282062'='0D1750';'18144D'='0D1750'}
 for($y=8;$y -lt 30;$y+=2){for($x=0;$x -lt 40;$x+=2){$c=$headPart.GetPixel($x,$y);$k=HexColor $c;if($c.A -and $crownRamp.ContainsKey($k)){Block $headPart $x $y (ColorValue $crownRamp[$k])}}}
 # Re-articulate the crown tips/ridges that were only one source pixel wide.
 foreach($p in @(@(16,8,'05052D'),@(24,8,'05052D'),@(16,10,'8887A0'),@(24,10,'8887A0'),@(20,12,'AAA7AA'),@(18,14,'AAA7AA'),@(16,16,'8887A0'),@(14,18,'62628A'),@(12,20,'424579'),@(28,12,'AAA7AA'),@(30,14,'8887A0'),@(6,14,'05052D'))){Block $headPart $p[0] $p[1] (ColorValue $p[2])}
 # Reconstruct only the narrow robe chest revealed when arms swing away.
 # Gauntlets, fingers and their outlines belong to the moving arm cells.
 $rows=@{
  30=@('000000','081827','4F6DA3','7095C4','7095C4','4F6DA3','081827','000000')
  32=@('000000','0D1750','4F6DA3','97B5DF','4F6DA3','4F6DA3','081827','000000')
  34=@('000000','05052D','18144D','282062','282062','0D1750','081827','000000')
  36=@('000000','05052D','18144D','282062','0D1750','05052D','081827','000000')
 }
 foreach($y in 30,32,34,36){for($i=0;$i -lt 8;$i++){Block $torso (14+2*$i) $y (ColorValue $rows[$y][$i])}}
 # Keep exposed breastplate and robe detail from the source. The near
 # fist starts at source x26; do not bake it into the chest.
 for($y=30;$y -lt 38;$y+=2){for($x=16;$x -lt 26;$x+=2){Block $torso (44-$x) $y (SourceColor $x (336+$y))}}
 foreach($x in 18,20,22,24,26){Block $torso $x 28 (ColorValue '081827')}
 for($y=28;$y -lt 34;$y+=2){for($x=30;$x -lt 40;$x+=2){$c=SourceColor $x (336+$y);if($c.A){Block $frontCap (44-$x) $y $c}}}
 Block $backCap 28 30 (ColorValue '081827');Block $backCap 30 30 (ColorValue '4F6DA3')
 Block $backCap 28 32 (ColorValue '0D1750');Block $backCap 30 32 (ColorValue '7095C4')
 # Grow glove seeds through the arm silhouette by one logical pixel so
 # the gauntlets cover hands and lower forearms in every bent/use pose.
 for($row=0;$row -lt 4;$row++){for($col=2;$col -lt 9;$col++){
  $dist=[int[,]]::new(20,28);$queue=[Collections.Generic.Queue[object]]::new()
  for($yy=0;$yy -lt 28;$yy++){for($xx=0;$xx -lt 20;$xx++){$dist[$xx,$yy]=99;if($hands.GetPixel($col*40+$xx*2,$row*56+$yy*2).A){$dist[$xx,$yy]=0;$queue.Enqueue(@($xx,$yy))}}}
  while($queue.Count){$p=$queue.Dequeue();$xx=$p[0];$yy=$p[1];if($dist[$xx,$yy] -ge 1){continue};foreach($d in @(@(-1,0),@(1,0),@(0,-1),@(0,1))){$nx=$xx+$d[0];$ny=$yy+$d[1];if($nx -ge 0 -and $nx -lt 20 -and $ny -ge 0 -and $ny -lt 28 -and $dist[$nx,$ny] -gt $dist[$xx,$yy]+1 -and $arms.GetPixel($col*40+$nx*2,$row*56+$ny*2).A){$dist[$nx,$ny]=$dist[$xx,$yy]+1;$queue.Enqueue(@($nx,$ny))}}}
  for($yy=0;$yy -lt 28;$yy++){for($xx=0;$xx -lt 20;$xx++){$x=$col*40+$xx*2;$y=$row*56+$yy*2;$c=$arms.GetPixel($x,$y);if(!$c.A){continue};$k=HexColor $c;$isGlove=$dist[$xx,$yy] -le 1;$map=if($isGlove){$glove}else{$sleeve};if(!$map.ContainsKey($k)){throw "Unknown template shade $k"};$value=$map[$k]
   # The gauntlet contour follows its moving silhouette, never the torso.
   if($isGlove){foreach($d in @(@(-1,0),@(1,0),@(0,-1),@(0,1))){$nx=$xx+$d[0];$ny=$yy+$d[1];if($nx -lt 0 -or $nx -ge 20 -or $ny -lt 0 -or $ny -ge 28 -or !$arms.GetPixel($col*40+$nx*2,$row*56+$ny*2).A){$value='081827'}}}
   Block $body $x $y (ColorValue $value)
  }}
  # A few occluded rear-arm cells are only two logical pixels wide. A
  # contour on every exposed side would erase their entire blue palm.
  $light=0;$bestScore=-1;$bestX=-1;$bestY=-1
  for($yy=0;$yy -lt 28;$yy++){for($xx=0;$xx -lt 20;$xx++){
   $x=$col*40+$xx*2;$y=$row*56+$yy*2;$c=$body.GetPixel($x,$y)
   if($c.A -and $c.R -gt 70){$light++}
   if(!$c.A -or !$hands.GetPixel($x,$y).A){continue};$score=0
   foreach($d in @(@(-1,0),@(1,0),@(0,-1),@(0,1))){$nx=$xx+$d[0];$ny=$yy+$d[1];if($nx -ge 0 -and $nx -lt 20 -and $ny -ge 0 -and $ny -lt 28 -and $arms.GetPixel($col*40+$nx*2,$row*56+$ny*2).A){$score++}}
   if($score -gt $bestScore){$bestScore=$score;$bestX=$x;$bestY=$y}
  }}
  if(!$light -and $bestX -ge 0){Block $body $bestX $bestY (ColorValue '7095C4')}
 }}
 $g=[Drawing.Graphics]::FromImage($body);foreach($r in 0,2){foreach($c in 0,1){$g.DrawImageUnscaled($torso,$c*40,$r*56)};$g.DrawImageUnscaled($frontCap,0,($r+1)*56);$g.DrawImageUnscaled($backCap,40,($r+1)*56)};$g.Dispose()
 $idleTop=56
 for($f=0;$f -lt 20;$f++){$top=56;for($y=0;$y -lt 56;$y+=2){for($x=0;$x -lt 40;$x+=2){if($reference.GetPixel($x,$f*56+$y).A){$top=[Math]::Min($top,$y)}}};if(!$f){$idleTop=$top};for($hy=0;$hy -lt 56;$hy++){for($hx=0;$hx -lt 40;$hx++){$c=$headPart.GetPixel($hx,$hy);if($c.A){$head.SetPixel($hx,$f*56+$hy+$top-$idleTop,$c)}}}
  $nf=if($f -eq 5){0}elseif($f -ge 6){$f-5}else{6}
  # Restore the moving waistband (36..39) on the legs, including a hidden
  # overlap row beneath the chest for the normal two-pixel torso bob.
  # Side gauntlets remain excluded from this central strip.
  for($y=36;$y -lt 56;$y+=2){for($x=6;$x -lt 40;$x+=2){if($y -lt 40 -and ($x -lt 16 -or $x -gt 24)){continue};if($y -eq 40 -and ($x -lt 16 -or $x -gt 28)){continue};$c=SourceColor $x ($nf*56+$y);if($c.A){Block $legs (44-$x) ($f*56+$y) $c}}}
  # The source arms obscure the near side of the waist. Reconstruct that
  # cloth here, across the FULL chest width, instead of leaving a notch.
  # Row 34 overlaps the chest during a two/four-pixel step-up body shift.
  $underCloth=@('000000','05052D','18144D','282062','4F6DA3','97B5DF','0D1750','000000')
  for($y=34;$y -le 40;$y+=2){for($x=14;$x -lt 30;$x+=2){
   if($legs.GetPixel($x,$f*56+$y).A){continue}
   $c=$legs.GetPixel($x,$f*56+38)
   if(!$c.A){$c=ColorValue $underCloth[($x-14)/2]}
   Block $legs $x ($f*56+$y) $c
  }}
  # Dark hidden underlap, a distinct horizontal belt, then the original
  # frame-specific robe fold. Never duplicate the lower row into the belt.
  $underlap=@('000000','05052D','18144D','18144D','0D1750','2A306A','05052D','000000')
  $belt=@('000000','05052D','18144D','000000','4F6DA3','97B5DF','4F6DA3','000000')
  foreach($y in 34,36){for($x=14;$x -lt 30;$x+=2){Block $legs $x ($f*56+$y) (ColorValue $underlap[($x-14)/2])}}
  for($x=14;$x -lt 30;$x+=2){Block $legs $x ($f*56+38) (ColorValue $belt[($x-14)/2])}
 }
 if(!$LegsOnly){$head.Save((Join-Path $OutputDirectory 'HydrisNecromancerHeadpiece_Head.png'));$body.Save((Join-Path $OutputDirectory 'HydrisNecromancerRobe_Body.png'))};$legs.Save((Join-Path $OutputDirectory 'HydrisNecromancerBoots_Legs.png'))
} finally{foreach($b in @($npc,$arms,$hands,$reference,$headPart,$torso,$frontCap,$backCap,$head,$body,$legs)){$b.Dispose()}}
function Write-Icon([string]$Name,[string]$Suffix){
 $sheet=[Drawing.Bitmap]::FromFile((Join-Path $OutputDirectory "${Name}_${Suffix}.png"))
 if($Suffix -eq 'Body'){$b=[Drawing.Bitmap]::new(40,56);$g=[Drawing.Graphics]::FromImage($b);foreach($cell in @(@(1,1),@(0,0),@(0,1))){$g.DrawImage($sheet,[Drawing.Rectangle]::new(0,0,40,56),$cell[0]*40,$cell[1]*56,40,56,[Drawing.GraphicsUnit]::Pixel)};$g.Dispose();$sheet.Dispose();$sheet=$b}
 $l=40;$t=56;$r=0;$bottom=0;for($y=0;$y -lt 56;$y++){for($x=0;$x -lt 40;$x++){if($sheet.GetPixel($x,$y).A){$l=[Math]::Min($l,$x);$t=[Math]::Min($t,$y);$r=[Math]::Max($r,$x+1);$bottom=[Math]::Max($bottom,$y+1)}}}
 $icon=$sheet.Clone([Drawing.Rectangle]::new($l,$t,$r-$l,$bottom-$t),[Drawing.Imaging.PixelFormat]::Format32bppArgb);$icon.Save((Join-Path $OutputDirectory "$Name.png"));$icon.Dispose();$sheet.Dispose();Write-Output "$Name icon: $($r-$l)x$($bottom-$t)"
}
if(!$LegsOnly){Write-Icon HydrisNecromancerHeadpiece Head;Write-Icon HydrisNecromancerRobe Body};Write-Icon HydrisNecromancerBoots Legs




