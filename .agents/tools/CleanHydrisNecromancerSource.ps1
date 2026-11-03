[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$InputPath,[Parameter(Mandatory=$true)][string]$OutputPath)
$ErrorActionPreference='Stop';Add-Type -AssemblyName System.Drawing
$source=[Drawing.Bitmap]::FromFile([IO.Path]::GetFullPath($InputPath));$result=[Drawing.Bitmap]::new($source.Width,$source.Height)
if($source.Width -ne 46 -or $source.Height -ne 840){throw 'Expected the 46x840 Hydris Necromancer NPC sheet'}
$palette=@('000000','05052D','0D1750','2A306A','424579','62628A','8887A0','AAA7AA','C0CFDF','1E0700','AA4D3C','E56D50','FF7D5A','18144D','282062','4F6DA3','97B5DF','081827','485B75','7095C4')
$colors=@($palette|ForEach-Object{[Drawing.ColorTranslator]::FromHtml('#'+$_)});$cache=@{}
try{for($y=0;$y -lt $source.Height;$y+=2){for($x=0;$x -lt $source.Width;$x+=2){
 $rr=0.0;$gg=0.0;$bb=0.0;$weight=0
 foreach($dy in 0,1){foreach($dx in 0,1){$p=$source.GetPixel($x+$dx,$y+$dy);$weight+=$p.A;$rr+=$p.R*$p.A;$gg+=$p.G*$p.A;$bb+=$p.B*$p.A}}
 if($weight -lt 384){continue};$rr/=$weight;$gg/=$weight;$bb/=$weight;$key="$rr,$gg,$bb"
 if($cache.ContainsKey($key)){$c=$cache[$key]}else{
  $best=0;$distance=[double]::MaxValue;for($i=0;$i -lt $colors.Count;$i++){$p=$colors[$i];$d=($rr-$p.R)*($rr-$p.R)+($gg-$p.G)*($gg-$p.G)+($bb-$p.B)*($bb-$p.B);if($d -lt $distance){$distance=$d;$best=$i}}
  $c=$colors[$best];$cache[$key]=$c
 };foreach($dy in 0,1){foreach($dx in 0,1){$result.SetPixel($x+$dx,$y+$dy,$c)}}
}};$result.Save([IO.Path]::GetFullPath($OutputPath))}finally{$source.Dispose();$result.Dispose()}
Write-Output 'Cleaned all 15 NPC frames: opaque/transparent alpha only, aligned 2x2 blocks, original blue/silver/skin palette.'
