[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$InputPath,[Parameter(Mandatory=$true)][string]$OriginalReferencePath,[Parameter(Mandatory=$true)][string]$OutputPath)
$ErrorActionPreference='Stop';Add-Type -AssemblyName System.Drawing
$b=[Drawing.Bitmap]::FromFile([IO.Path]::GetFullPath($InputPath));$raw=[Drawing.Bitmap]::FromFile([IO.Path]::GetFullPath($OriginalReferencePath))
if($b.Width -ne 46 -or $b.Height -ne 840 -or $raw.Size -ne $b.Size){throw 'Expected matching 46x840 Hydris NPC sheets'}
function C([string]$s){[Drawing.ColorTranslator]::FromHtml('#'+$s)}
function Block([int]$x,[int]$y,$c){foreach($dy in 0,1){foreach($dx in 0,1){$b.SetPixel($x+$dx,$y+$dy,$c)}}}
$poses=ConvertFrom-Json '[[[18,50],[32,50]],[[28,50]],[[30,46],[24,50]],[[32,48],[22,50]],[[22,50],[32,50]],[[22,50],[30,50]],[[30,50]],[[28,50]],[[28,50]],[[30,48]],[[32,48]],[[32,50]],[[32,50]],[[32,50]],[[30,50]]]'
$hex=@('000000','05052D','0D1750','18144D','282062','2A306A','4F6DA3','7095C4','081827','485B75')
$palette=@($hex|ForEach-Object{C $_});$belt=@('000000','05052D','18144D','000000','4F6DA3','97B5DF','4F6DA3','000000')
try{for($f=0;$f -lt 15;$f++){
 $protected=@{};foreach($p in $poses[$f]){for($i=0;$i -lt 3;$i++){$protected["$([int]$p[0]-6+$i*2),$($p[1])"]=$true}}
 # The original has a horizontal belt accent, not an extension of the
 # vertical fold below. Source fists beside this central area stay intact.
 for($x=20;$x -lt 30;$x+=2){$sx=44-$x;if($b.GetPixel($sx,$f*56+38).A){Block $sx ($f*56+38) (C $belt[($x-14)/2])}}
 for($y=40;$y -le 48;$y+=2){for($x=14;$x -lt 30;$x+=2){
  if($protected.ContainsKey("$x,$y")){continue};$sx=44-$x;if(!$b.GetPixel($sx,$f*56+$y).A){continue}
  # Preserve the original thin blue/purple fold lines by selecting their
  # strongest opaque source texel before snapping it onto the 2x2 grid.
  # Averaging that line with black had erased a full logical detail row.
  $sample=[Drawing.Color]::Black;$best=-1
  foreach($dy in 0,1){foreach($dx in 0,1){$p=$raw.GetPixel($sx+$dx,$f*56+$y+$dy);if($p.A -lt 128){continue};$score=[double]$p.B+0.3*$p.R+0.2*$p.G;if($score -gt $best){$best=$score;$sample=$p}}}
  $index=0;$distance=[double]::MaxValue
  for($i=0;$i -lt $palette.Count;$i++){$p=$palette[$i];$d=($sample.R-$p.R)*($sample.R-$p.R)+($sample.G-$p.G)*($sample.G-$p.G)+($sample.B-$p.B)*($sample.B-$p.B);if($d -lt $distance){$distance=$d;$index=$i}}
  $color=$palette[$index]
  # Dark blue fabric lines must not collapse into the black outline. Lift
  # the two original shadow tiers onto distinct solid palette steps.
  if($sample.R -lt 35 -and $sample.G -lt 35 -and $sample.B -gt $sample.R+7 -and $sample.B -gt $sample.G+12 -and !($sample.G -gt $sample.R*2 -and $sample.G -gt 8)){
   if($sample.B -ge 45){$color=C '18144D'}elseif($sample.B -ge 12){$color=C '05052D'}
  }
  Block $sx ($f*56+$y) $color
 }}
};$b.Save([IO.Path]::GetFullPath($OutputPath))}finally{$b.Dispose();$raw.Dispose()}
Write-Output 'Restored original robe fold/shadow rows and distinct belt; protected all boot color bands.'
