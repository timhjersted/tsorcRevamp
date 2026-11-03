[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$RepoRoot,[string]$OutputDirectory,[string]$TemplatePath)
$ErrorActionPreference='Stop';Add-Type -AssemblyName System.Drawing
if(!$OutputDirectory){$OutputDirectory=Join-Path $RepoRoot 'Content\Items\Armor\Enemy\SkeletonSet'}
if(!$TemplatePath){$TemplatePath=Join-Path $RepoRoot '.agents\tools\assets\SkeletonHeadTemplate.png'}
New-Item -ItemType Directory -Force $OutputDirectory|Out-Null
$source=[Drawing.Bitmap]::FromFile($TemplatePath);$head=[Drawing.Bitmap]::new(40,1120)
$white=[Drawing.Bitmap]::new(40,1120);$red=[Drawing.Bitmap]::new(40,1120);$blue=[Drawing.Bitmap]::new(40,1120)
if($source.Width -ne 40 -or $source.Height -ne 1120){throw 'Expected standard head sheet'}
$outline=[Drawing.Color]::FromArgb(255,31,29,26)
try{for($frame=0;$frame -lt 20;$frame++){$eyes=0
 for($y=0;$y -lt 56;$y+=2){for($x=0;$x -lt 40;$x+=2){$c=$source.GetPixel($x,$frame*56+$y);if(!$c.A){continue};$edge=$false
  foreach($d in @(@(-2,0),@(2,0),@(0,-2),@(0,2))){$nx=$x+$d[0];$ny=$y+$d[1];if($nx -lt 0 -or $nx -ge 40 -or $ny -lt 0 -or $ny -ge 56 -or !$source.GetPixel($nx,$frame*56+$ny).A){$edge=$true}}
  $eye=$c.R -eq 28 -and $c.G -eq 27 -and $c.B -eq 19
  foreach($dy in 0,1){foreach($dx in 0,1){$xx=$x+$dx;$yy=$frame*56+$y+$dy;$head.SetPixel($xx,$yy,$(if($edge){$outline}else{$c}));if($eye){$white.SetPixel($xx,$yy,[Drawing.Color]::White);$red.SetPixel($xx,$yy,[Drawing.Color]::FromArgb(255,255,48,32));$blue.SetPixel($xx,$yy,[Drawing.Color]::FromArgb(255,64,176,255))}}}
  if($eye){$eyes++}
 }};if($eyes -ne 1){throw "Expected one 2x2 eye in head frame $frame"}
}
$head.Save((Join-Path $OutputDirectory 'Skeleton_Head.png'));$white.Save((Join-Path $OutputDirectory 'Skeleton_Head_Glow.png'));$red.Save((Join-Path $OutputDirectory 'Skeleton_Head_Glow_Red.png'));$blue.Save((Join-Path $OutputDirectory 'Skeleton_Head_Glow_Blue.png'))
}finally{foreach($b in @($source,$head,$white,$red,$blue)){$b.Dispose()}}
Write-Output 'Matched head perimeter to 1F1D1A; generated aligned white, red and blue eye masks.'
