[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$FireBodyPath,[Parameter(Mandatory=$true)][string]$BasiliskBodyPath,[Parameter(Mandatory=$true)][string]$OutputPath)
$ErrorActionPreference='Stop';Add-Type -AssemblyName System.Drawing
$source=[Drawing.Bitmap]::FromFile([IO.Path]::GetFullPath($FireBodyPath));$inputBody=[Drawing.Bitmap]::FromFile([IO.Path]::GetFullPath($BasiliskBodyPath))
$result=[Drawing.Bitmap]::new($inputBody.Width,$inputBody.Height,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
for($y=0;$y -lt $inputBody.Height;$y++){for($x=0;$x -lt 80;$x++){$result.SetPixel($x,$y,$inputBody.GetPixel($x,$y))}};$inputBody.Dispose()
if($source.Width -ne 360 -or $source.Height -ne 224 -or $result.Width -ne 360 -or $result.Height -ne 224){throw 'Expected native 360x224 body sheets'}
# Existing Basilisk slate/royal-blue palette; the claw color matches its
# bright-blue head highlights. Each Fire Lurker shade has its own tint.
$map=@{'300400'='262637';'570505'='3C3C56';'7C0C00'='484966';'710707'='273068';'941800'='263981';'BC1400'='686894';'E06C0C'='9890E1';'E0B47C'='7F96FF'}
try{for($y=0;$y -lt 224;$y++){for($x=80;$x -lt 360;$x++){$c=$source.GetPixel($x,$y)
 if(!$c.A){$result.SetPixel($x,$y,[Drawing.Color]::Transparent);continue}
 $key='{0:X2}{1:X2}{2:X2}' -f $c.R,$c.G,$c.B;if(!$map.ContainsKey($key)){throw "Unmapped Fire Lurker arm color: $key"}
 $t=[Drawing.ColorTranslator]::FromHtml('#'+$map[$key]);$result.SetPixel($x,$y,[Drawing.Color]::FromArgb($c.A,$t.R,$t.G,$t.B))
}};$result.Save([IO.Path]::GetFullPath($OutputPath),[Drawing.Imaging.ImageFormat]::Png)}finally{$source.Dispose();$result.Dispose()}
& (Join-Path $PSScriptRoot 'MoveBasiliskShouldersToArms.ps1') -BasiliskBodyPath $OutputPath -OutputPath $OutputPath
Write-Output 'Transplanted Fire Lurker arms with Basilisk tints and diagonal moving shoulder pads; torso preserved.'

