function Paint-FireLurkerArms($Body,$Arms,$Hands,[string]$ForearmPath){
 $forearm=[Drawing.Bitmap]::FromFile($ForearmPath)
 if($forearm.Width -ne 22 -or $forearm.Height -ne 10){throw 'Expected the exact 22x10 curled-forearm cutout'}
 $palm=$forearm.Clone([Drawing.Rectangle]::new(12,0,10,10),[Drawing.Imaging.PixelFormat]::Format32bppArgb)
 $ramp=@{'201C1C'='300400';'242131'='300400';'3D3336'='570505';'9C8F9B'='7C0C00';'D4C7D4'='BC1400';'DDD2DD'='E06C0C'}
 $record=@()
 try{for($row=0;$row -lt 4;$row++){for($col=2;$col -lt 9;$col++){
  $count=0;$minX=40;$maxX=0;$minY=56;$maxY=0
  for($y=0;$y -lt 56;$y+=2){for($x=0;$x -lt 40;$x+=2){$c=$Arms.GetPixel($col*40+$x,$row*56+$y);if(!$c.A){continue};$count++;$key=HexColor $c;Block $Body ($col*40+$x) ($row*56+$y) (C $ramp[$key])
   if($Hands.GetPixel($col*40+$x,$row*56+$y).A){$minX=[Math]::Min($minX,$x);$maxX=[Math]::Max($maxX,$x);$minY=[Math]::Min($minY,$y);$maxY=[Math]::Max($maxY,$y)}
  }}
  if(!$count){continue}
  $frontFull=($row -eq 0 -and $col -eq 2) -or ($row -eq 1 -and $col -ge 3 -and $col -le 6)
  $backFull=($row -eq 2 -and $col -eq 2) -or ($row -eq 3 -and $col -ge 3 -and $col -le 6)
  if($frontFull -or $backFull){
   for($y=0;$y -lt 56;$y+=2){for($x=0;$x -lt 40;$x+=2){Block $Body ($col*40+$x) ($row*56+$y) ([Drawing.Color]::Transparent)}}
   if($frontFull){$ox=if($col -eq 2){6}elseif($col -eq 3){6}elseif($col -eq 4){4}elseif($col -eq 5){8}else{10}}
   else{$ox=if($col -eq 2){14}elseif($col -eq 3){12}elseif($col -eq 4){16}elseif($col -eq 5){12}else{10}}
   Blit $Body $forearm ($col*40+$ox) ($row*56+30)
   $record+=@{Column=$col;Row=$row;Mode='ExactForearm';X=$ox;Y=30;QuarterTurns=0}
  }else{
   if($minX -eq 40){throw "Missing hand anchor in arm cell $col,$row"}
   # Remove the old fist, including its contour. The source palm supplies
   # the complete outline, two separate curled fingers and both claw tips.
   for($y=0;$y -lt 56;$y+=2){for($x=0;$x -lt 40;$x+=2){if($Hands.GetPixel($col*40+$x,$row*56+$y).A){Block $Body ($col*40+$x) ($row*56+$y) ([Drawing.Color]::Transparent)}}}
   $cx=[int](2*[Math]::Round(($minX+$maxX)/4));$cy=[int](2*[Math]::Round(($minY+$maxY)/4))
   $back=$col -eq 8 -or ($row -ge 2 -and $col -lt 7);$jx=if($back){26}else{15};$jy=if($back){30}else{28};$vx=$cx-$jx;$vy=$cy-$jy
   $turn=if([Math]::Abs($vx) -gt [Math]::Abs($vy)){if($vx -ge 0){0}else{2}}else{if($vy -ge 0){1}else{3}}
   $ox=$cx-4;$oy=$cy-4
   for($y=0;$y -lt 5;$y++){for($x=0;$x -lt 5;$x++){$c=$palm.GetPixel($x*2,$y*2);if(!$c.A){continue};switch($turn){0{$tx=$x;$ty=$y}1{$tx=4-$y;$ty=$x}2{$tx=4-$x;$ty=4-$y}3{$tx=$y;$ty=4-$x}}
    $dx=$ox+2*$tx;$dy=$oy+2*$ty;if($dx -lt 0 -or $dx -gt 38 -or $dy -lt 0 -or $dy -gt 54){throw "Source hand clipped in $col,$row"};Block $Body ($col*40+$dx) ($row*56+$dy) $c
   }}
   $record+=@{Column=$col;Row=$row;Mode='ExactPalm';X=$ox;Y=$oy;QuarterTurns=$turn}
  }
 }};return $record}finally{$palm.Dispose();$forearm.Dispose()}
}
