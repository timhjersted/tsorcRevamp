[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$RepoRoot,[string]$OutputDirectory,[string]$TemplatePath)
$ErrorActionPreference='Stop';Add-Type -AssemblyName System.Drawing
if(!$OutputDirectory){$OutputDirectory=Join-Path $RepoRoot 'Content\Items\Armor\Enemy\SkeletonSet'}
if(!$TemplatePath){$TemplatePath=Join-Path $RepoRoot '.agents\tools\assets\SkeletonIceLegsTemplate.png'}
New-Item -ItemType Directory -Force $OutputDirectory|Out-Null
$src=[Drawing.Bitmap]::FromFile($TemplatePath);$dst=[Drawing.Bitmap]::new(40,1120)
if($src.Width -ne 40 -or $src.Height -ne 840){throw 'Expected original 40x840 Ice Skeleton sheet'}
# Use the supplied rib cage's exact five-color bone ramp. Blue-grey shafts
# and teal foot pixels become one bone material, retaining the NPC shading.
$palette=@{
 '181E24'='1F1D1A';'2C2D3A'='1F1D1A';'18282C'='1F1D1A'
 '324D51'='37342D';'375756'='37342D';'3D4D5B'='44413C'
 '296B66'='44413C';'276862'='5D5A52';'5F7181'='5D5A52'
 '60868D'='5D5A52';'758899'='7B7872';'829099'='7B7872'
 '8A9FB2'='7B7872';'3A9A92'='7B7872';'82B5BE'='7B7872'
}
try{for($f=0;$f -lt 20;$f++){
 $nf=if($f -eq 5){0}elseif($f -ge 6){$f-5}else{6}
 $hip=if($nf -in @(2,3,4,9,10,11)){36}else{38}
 for($y=$hip;$y -lt 56;$y+=2){for($x=0;$x -lt 40;$x+=2){
  # The first source row also contains the stationary fists at either side.
  # Only the central waist belongs on the legs; hands remain on the arm sheet.
  if($y -eq $hip -and ($x -lt 12 -or $x -gt 22)){continue}
  $c=$src.GetPixel($x,$nf*56+$y);if(!$c.A){continue};$key='{0:X2}{1:X2}{2:X2}' -f $c.R,$c.G,$c.B
  if(!$palette.ContainsKey($key)){throw "Unknown Ice Skeleton leg color: $key at $nf,$x,$y"}
  $mapped=[Drawing.ColorTranslator]::FromHtml('#'+$palette[$key]);foreach($dy in 0,1){foreach($dx in 0,1){$dst.SetPixel(38-$x+$dx,$f*56+$y+$dy,$mapped)}}
 }}
};$dst.Save((Join-Path $OutputDirectory 'Skeleton_Legs.png'))}finally{$src.Dispose();$dst.Dispose()}
Write-Output 'Ice Skeleton leg/foot poses copied without resizing; recolored to the rib-cage bone palette.'
