[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$InputPath,[Parameter(Mandatory=$true)][string]$OutputPath)
$ErrorActionPreference='Stop';Add-Type -AssemblyName System.Drawing
$b=[Drawing.Bitmap]::FromFile([IO.Path]::GetFullPath($InputPath))
if($b.Width -ne 46 -or $b.Height -ne 840){throw 'Expected 15 native 46x56 NPC cells'}
function C([string]$s){[Drawing.ColorTranslator]::FromHtml('#'+$s)}
function Block([int]$x,[int]$y,$color){foreach($dy in 0,1){foreach($dx in 0,1){$b.SetPixel($x+$dx,$y+$dy,$color)}}}
# Foot toe/upper-sole anchors in RIGHT-facing equipment coordinates.
# Each pose uses the same three color blocks immediately before its toe.
$poses=ConvertFrom-Json '[[[18,50],[32,50]],[[28,50]],[[30,46],[24,50]],[[32,48],[22,50]],[[22,50],[32,50]],[[22,50],[30,50]],[[30,50]],[[28,50]],[[28,50]],[[30,48]],[[32,48]],[[32,50]],[[32,50]],[[32,50]],[[30,50]]]'
$triad=@('485B75','7095C4','4F6DA3');$waist=@('000000','05052D','18144D','282062','485B75','7095C4','4F6DA3','081827')
try{for($f=0;$f -lt 15;$f++){
 # Only the boot bands are recolored. Robe and waist artwork are preserved.
 foreach($p in $poses[$f]){$toe=[int]$p[0];$y=[int]$p[1];for($x=$toe-10;$x -lt $toe;$x+=2){
  if($bands.ContainsKey("$x,$y")){continue};$sx=44-$x;$c=$b.GetPixel($sx,$f*56+$y)
  if($c.A -and $c.R -ge 40 -and $c.B -gt $c.R){Block $sx ($f*56+$y) (C '081827')}
 }}
 
 foreach($p in $poses[$f]){$toe=[int]$p[0];$y=[int]$p[1]
  for($i=0;$i -lt 3;$i++){$x=$toe-6+$i*2;$sx=44-$x;if(!$b.GetPixel($sx,$f*56+$y).A){throw "Foot anchor outside silhouette: source $f $x,$y"};Block $sx ($f*56+$y) (C $triad[$i])}
 }
};$b.Save([IO.Path]::GetFullPath($OutputPath))}finally{$b.Dispose()}
Write-Output 'Stabilized boot color triplets across all 15 NPC frames; robe shading and silhouettes preserved.'


