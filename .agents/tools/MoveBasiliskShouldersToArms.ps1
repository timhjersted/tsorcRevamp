[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$BasiliskBodyPath,[Parameter(Mandatory=$true)][string]$OutputPath)
$ErrorActionPreference='Stop';Add-Type -AssemblyName System.Drawing
$inputBody=[Drawing.Bitmap]::FromFile([IO.Path]::GetFullPath($BasiliskBodyPath))
if($inputBody.Width -ne 360 -or $inputBody.Height -ne 224){$inputBody.Dispose();throw 'Expected native 360x224 body sheet'}
$body=[Drawing.Bitmap]::new(360,224,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
for($y=0;$y -lt 224;$y++){for($x=0;$x -lt 360;$x++){$body.SetPixel($x,$y,$inputBody.GetPixel($x,$y))}};$inputBody.Dispose()
$palette=@{B='2D251D';D='262637';s='3C3C56';m='484966';h='686894';L='9890E1';u='263981'}
# Source NPC frame 0, front: x46,44,42,40 / y32,34,36,38.
# Back: x26,24,22 / y30,32,34,36. Preserve its diagonal stepped outline.
$frontPattern=@('..Bu','.BuB','BuLB','BmBh');$backPattern=@('muB','uB.','DhD','shD')
# Exact palm placements from the Fire Lurker authoring manifest. The palm's
# 5x5 alpha mask is the rightmost 10x10 portion of its 22x10 forearm template.
# Paint pads behind these pixels, including folded hands close to the joint.
$palmMask=@('..##.','#####','#####','#####','#####')
$palms=@{
 '2,0'=@(18,30,0);'3,0'=@(6,16,3);'4,0'=@(20,16,0);'5,0'=@(24,30,0);'6,0'=@(20,36,1);'7,0'=@(8,36,1);'8,0'=@(22,36,1)
 '2,1'=@(6,16,3);'3,1'=@(18,30,0);'4,1'=@(16,30,0);'5,1'=@(20,30,0);'6,1'=@(22,30,0);'7,1'=@(8,32,1);'8,1'=@(22,32,1)
 '2,2'=@(26,30,0);'3,2'=@(26,30,1);'4,2'=@(24,34,1);'7,2'=@(8,32,1);'8,2'=@(22,32,1)
 '2,3'=@(26,16,3);'3,3'=@(24,30,0);'4,3'=@(28,30,0);'5,3'=@(24,30,0);'6,3'=@(22,30,0);'7,3'=@(8,28,1);'8,3'=@(22,28,1)
}
function Clear-Region([int]$X,[int]$Y,[int]$W,[int]$H){for($yy=$Y;$yy -lt $Y+$H;$yy++){for($xx=$X;$xx -lt $X+$W;$xx++){$body.SetPixel($xx,$yy,[Drawing.Color]::Transparent)}}}
function Paint-Pad([int]$Col,[int]$Row,[bool]$Front){
 $rows=if($Front){$frontPattern}else{$backPattern};$left=if($Front){10}else{24}
 for($j=0;$j -lt 4;$j++){for($i=0;$i -lt $rows[$j].Length;$i++){$key=[string]$rows[$j][$i];if($key -eq '.'){continue};$color=[Drawing.ColorTranslator]::FromHtml('#'+$palette[$key]);foreach($dy in 0,1){foreach($dx in 0,1){$body.SetPixel($Col*40+$left+$i*2+$dx,$Row*56+26+$j*2+$dy,$color)}}}}
}
try{
 # Empty both front/back shoulder-cap cells, for both torso variants.
 foreach($row in 1,3){Clear-Region 0 ($row*56) 80 56}
 $count=0
 for($row=0;$row -lt 4;$row++){for($col=2;$col -lt 9;$col++){
  $active=$false;for($y=0;$y -lt 56 -and !$active;$y+=2){for($x=0;$x -lt 40;$x+=2){if($body.GetPixel($col*40+$x,$row*56+$y).A){$active=$true;break}}};if(!$active){continue}
  $isFront=if($col -eq 7){$true}elseif($col -eq 8){$false}else{$row -lt 2}
  $p=$palms["$col,$row"];$handPixels=@()
  for($hy=0;$hy -lt 5;$hy++){for($hx=0;$hx -lt 5;$hx++){if($palmMask[$hy][$hx] -ne '#'){continue};$tx=$hx;$ty=$hy
   switch($p[2]){1{$tx=4-$hy;$ty=$hx}2{$tx=4-$hx;$ty=4-$hy}3{$tx=$hy;$ty=4-$hx}}
   foreach($dy in 0,1){foreach($dx in 0,1){$px=$col*40+$p[0]+2*$tx+$dx;$py=$row*56+$p[1]+2*$ty+$dy;$c=$body.GetPixel($px,$py);if($c.A){$handPixels+=,@($px,$py,$c)}}}
  }}
  # Keep the upper arm underneath the diagonal pad: transparent corners in
  # the pad are not holes in the arm. Only trim the old composite shoulder
  # protrusion above the joint, retaining the current sheet's shortened tops.
  if($col -eq 7){
   Clear-Region ($col*40+6) ($row*56) 12 26
   if($row -gt 0){Clear-Region ($col*40+6) ($row*56+30-2*$row) 2 2}
  }
  Paint-Pad $col $row $isFront
  foreach($pixel in $handPixels){$body.SetPixel($pixel[0],$pixel[1],$pixel[2])};$count++
 }}
 if($count -ne 26){throw "Expected 26 occupied arm cells, found $count"}
 $body.Save([IO.Path]::GetFullPath($OutputPath),[Drawing.Imaging.ImageFormat]::Png)
}finally{$body.Dispose()}
Write-Output 'Moved source-shaped diagonal shoulders onto all 26 arm cells; separate caps cleared.'


