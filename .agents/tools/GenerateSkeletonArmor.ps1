[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$RepoRoot,[string]$OutputDirectory,[string]$AssetsDirectory)
$ErrorActionPreference='Stop';Add-Type -AssemblyName System.Drawing
if(!$OutputDirectory){$OutputDirectory=Join-Path $RepoRoot 'Content\Items\Armor\Enemy\SkeletonSet'}
if(!$AssetsDirectory){$AssetsDirectory=Join-Path $RepoRoot '.agents\tools\assets'}
& (Join-Path $PSScriptRoot 'GenerateSkeletonLegs.ps1') -RepoRoot $RepoRoot -OutputDirectory $OutputDirectory -TemplatePath (Join-Path $AssetsDirectory 'SkeletonIceLegsTemplate.png')
& (Join-Path $PSScriptRoot 'GenerateSkeletonHead.ps1') -RepoRoot $RepoRoot -OutputDirectory $OutputDirectory -TemplatePath (Join-Path $AssetsDirectory 'SkeletonHeadTemplate.png')
Copy-Item (Join-Path $AssetsDirectory 'SkeletonBodyTemplate.png') (Join-Path $OutputDirectory 'Skeleton_Body.png') -Force
foreach($e in @(@('SkeletonHeadpiece','Head'),@('SkeletonRibcage','Body'),@('SkeletonLegBones','Legs'))){$b=[Drawing.Bitmap]::FromFile((Join-Path $OutputDirectory ('Skeleton_'+$e[1]+'.png')));if($e[1] -eq 'Head'){$mask=[Drawing.Bitmap]::FromFile((Join-Path $OutputDirectory 'Skeleton_Head_Glow_Red.png'));$g=[Drawing.Graphics]::FromImage($b);$g.DrawImageUnscaled($mask,0,0);$g.Dispose();$mask.Dispose()};$l=40;$t=56;$r=0;$bottom=0;for($y=0;$y -lt 56;$y++){for($x=0;$x -lt 40;$x++){if($b.GetPixel($x,$y).A){$l=[Math]::Min($l,$x);$t=[Math]::Min($t,$y);$r=[Math]::Max($r,$x+1);$bottom=[Math]::Max($bottom,$y+1)}}};$icon=$b.Clone([Drawing.Rectangle]::new($l,$t,$r-$l,$bottom-$t),[Drawing.Imaging.PixelFormat]::Format32bppArgb);$icon.Save((Join-Path $OutputDirectory ($e[0]+'.png')));Write-Output "$($e[0]) icon $($r-$l)x$($bottom-$t)";$b.Dispose();$icon.Dispose()}



