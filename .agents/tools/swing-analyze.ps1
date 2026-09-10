<#
.SYNOPSIS
  Turns a puppet-attack telemetry run into a swing chart plus animation-quality metrics.

.DESCRIPTION
  Reads the JSONL written by PuppetAttackTelemetry (enable in game with /swingarm log) and renders,
  for each attack run:

    Panel A  Onion-skin blade arc. The blade drawn hand->tip once per frame in NPC-local space,
             coloured windup / active-hitbox / recovery. This is the arc an animator judges.
    Panel B  Blade angle over time.
    Panel C  Angular velocity (deg/tick). Its shape is what reads as heavy vs snappy vs weightless.
    Panel D  Angular acceleration. Spikes here are visible snapping.

  The blade angle is taken from the logged hand/tip geometry rather than the raw rotation field, so
  it already folds in facing, mirroring and every draw-time offset - it is what the player sees.

  The reference bands in the metric block are discussion heuristics drawn from Souls-style melee,
  not rules. A boss deliberately built to feel wrong will fail them, and that can be correct.

.EXAMPLE
  powershell -File .agents/tools/swing-analyze.ps1
  powershell -File .agents/tools/swing-analyze.ps1 -Attack Cinderfall -Top 3
#>
[CmdletBinding()]
param(
    [string]$LogFile,
    [string]$Attack,
    [string]$Puppet,
    [int]$Run = -1,
    [int]$Top = 1,
    [string]$OutDir
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
if (-not $OutDir) { $OutDir = Join-Path $repoRoot 'tsorcDocs\SwingReports' }
if (-not (Test-Path $OutDir)) { New-Item -ItemType Directory -Path $OutDir | Out-Null }

if (-not $LogFile) {
    $logDir = Join-Path $env:USERPROFILE 'Documents\My Games\Terraria\tModLoader\Logs'
    $candidate = Get-ChildItem -Path $logDir -Filter 'tsorcRevamp-puppet-attack-*.jsonl' -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if (-not $candidate) {
        Write-Host "No telemetry file found in $logDir" -ForegroundColor Yellow
        Write-Host "Enable logging in game with /swingarm log, fight something, then re-run this." -ForegroundColor Yellow
        return
    }
    $LogFile = $candidate.FullName
}
Write-Host "log: $LogFile"

# Substring pre-filter so ConvertFrom-Json only runs on rows we actually want.
$frames = New-Object System.Collections.ArrayList
foreach ($line in [System.IO.File]::ReadLines($LogFile)) {
    if ($line -notmatch 'render_frame') { continue }
    [void]$frames.Add(($line | ConvertFrom-Json))
}
if ($frames.Count -eq 0) {
    Write-Host 'No render_frame rows in that file.' -ForegroundColor Yellow
    return
}

$sel = $frames
if ($Attack) { $sel = $sel | Where-Object { $_.attack -like "*$Attack*" } }
if ($Puppet) { $sel = $sel | Where-Object { $_.puppet -like "*$Puppet*" } }
if ($Run -ge 0) { $sel = $sel | Where-Object { $_.run -eq $Run } }
if (-not $sel -or @($sel).Count -eq 0) {
    Write-Host 'Nothing matched those filters.' -ForegroundColor Yellow
    return
}

$runs = $sel | Group-Object run | Sort-Object { [int]$_.Name } -Descending | Select-Object -First $Top

function Get-Unwrapped {
    param([double[]]$Series)
    # A facing flip or a +-180 crossing otherwise shows up as a fake ~300 deg/tick spike.
    $out = New-Object 'double[]' $Series.Length
    if ($Series.Length -eq 0) { return $out }
    $out[0] = $Series[0]
    for ($i = 1; $i -lt $Series.Length; $i++) {
        $delta = $Series[$i] - $Series[$i - 1]
        while ($delta -gt 180) { $delta -= 360 }
        while ($delta -lt -180) { $delta += 360 }
        $out[$i] = $out[$i - 1] + $delta
    }
    return $out
}

function Get-Band {
    param([double]$Value, [double]$Lo, [double]$Hi, [string]$LowText, [string]$OkText, [string]$HighText)
    if ($Value -lt $Lo) { return $LowText }
    if ($Value -gt $Hi) { return $HighText }
    return $OkText
}

function Get-Ms {
    param([double]$Ticks)
    return [Math]::Round($Ticks / 60.0 * 1000.0)
}

function Draw-Series {
    param(
        $Graphics, $Pen, $Font, $LabelBrush,
        [int]$X, [int]$Y, [int]$W, [int]$H,
        [string]$Title, [double[]]$Series, $LineColor,
        [int]$ActiveFrom, [int]$ActiveTo, [int]$Count
    )
    $Graphics.DrawString($Title, $Font, $LabelBrush, $X, $Y - 2)
    $Graphics.DrawRectangle($Pen, $X, $Y + 16, $W, $H)

    $lo = ($Series | Measure-Object -Minimum).Minimum
    $hi = ($Series | Measure-Object -Maximum).Maximum
    if (($hi - $lo) -lt 1e-6) { $hi = $lo + 1 }

    if ($ActiveFrom -ge 0 -and $Count -gt 1) {
        $bx0 = $X + $W * ($ActiveFrom / [double]($Count - 1))
        $bx1 = $X + $W * ($ActiveTo / [double]($Count - 1))
        $band = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(46, 235, 70, 70))
        $Graphics.FillRectangle($band, $bx0, $Y + 16, [Math]::Max(1, $bx1 - $bx0), $H)
        $band.Dispose()
    }

    if (0 -ge $lo -and 0 -le $hi) {
        $zeroY = $Y + 16 + $H * (1 - ((0 - $lo) / ($hi - $lo)))
        $Graphics.DrawLine($Pen, $X, $zeroY, $X + $W, $zeroY)
    }

    $linePen = New-Object System.Drawing.Pen $LineColor, 1.8
    for ($i = 1; $i -lt $Series.Length; $i++) {
        $x0 = $X + $W * (($i - 1) / [double]($Series.Length - 1))
        $x1 = $X + $W * ($i / [double]($Series.Length - 1))
        $y0 = $Y + 16 + $H * (1 - (($Series[$i - 1] - $lo) / ($hi - $lo)))
        $y1 = $Y + 16 + $H * (1 - (($Series[$i] - $lo) / ($hi - $lo)))
        $Graphics.DrawLine($linePen, $x0, $y0, $x1, $y1)
    }
    $linePen.Dispose()

    $Graphics.DrawString(('{0:N0}' -f $hi), $Font, $LabelBrush, $X + $W + 4, $Y + 12)
    $Graphics.DrawString(('{0:N0}' -f $lo), $Font, $LabelBrush, $X + $W + 4, $Y + 16 + $H - 14)
}

foreach ($group in $runs) {
    $f = @($group.Group | Sort-Object tick)
    $n = $f.Count
    if ($n -lt 3) { continue }

    $attackName = $f[0].attack
    $puppetName = $f[0].puppet
    $runId = $f[0].run

    $raw = New-Object 'double[]' $n
    for ($i = 0; $i -lt $n; $i++) {
        $dx = $f[$i].visualTip[0] - $f[$i].hand[0]
        $dy = $f[$i].visualTip[1] - $f[$i].hand[1]
        $raw[$i] = [Math]::Atan2($dy, $dx) * 180.0 / [Math]::PI
    }
    $ang = Get-Unwrapped -Series $raw

    $vel = New-Object 'double[]' $n
    for ($i = 1; $i -lt $n; $i++) { $vel[$i] = $ang[$i] - $ang[$i - 1] }
    $acc = New-Object 'double[]' $n
    for ($i = 2; $i -lt $n; $i++) { $acc[$i] = $vel[$i] - $vel[$i - 1] }

    $firstActive = -1
    $lastActive = -1
    $activeCount = 0
    for ($i = 0; $i -lt $n; $i++) {
        if ($f[$i].bladeArmed) {
            if ($firstActive -lt 0) { $firstActive = $i }
            $lastActive = $i
            $activeCount++
        }
    }

    # Segment by the logged phase name, not by bladeArmed. A run spans telegraph -> attack ->
    # recovery, and the damage window is usually a sub-range of the attack rather than the whole of
    # it. Measuring sweep across the entire run instead nets out to ~0, because the telegraph raises
    # the weapon and the recovery lowers it back to almost the same angle.
    $swingFrom = -1
    $swingTo = -1
    $windup = 0
    $recovery = 0
    for ($i = 0; $i -lt $n; $i++) {
        $phaseName = [string]$f[$i].phase
        if ($phaseName -like '*Telegraph*') { $windup++ }
        elseif ($phaseName -like '*Recovery*' -or $phaseName -like '*Pause*') { $recovery++ }
        else {
            if ($swingFrom -lt 0) { $swingFrom = $i }
            $swingTo = $i
        }
    }
    # Fall back to the damage window, then to the whole run, if phase names are absent.
    if ($swingFrom -lt 0) {
        if ($firstActive -ge 0) { $swingFrom = $firstActive; $swingTo = $lastActive }
        else { $swingFrom = 0; $swingTo = $n - 1 }
    }
    $swingLen = [Math]::Max(1, $swingTo - $swingFrom)

    $sweepNet = [Math]::Abs($ang[$swingTo] - $ang[$swingFrom])
    $sweepTotal = 0.0
    for ($i = $swingFrom + 1; $i -le $swingTo; $i++) { $sweepTotal += [Math]::Abs($vel[$i]) }

    $peakVel = 0.0
    $peakAt = $swingFrom
    for ($i = $swingFrom + 1; $i -le $swingTo; $i++) {
        if ([Math]::Abs($vel[$i]) -gt $peakVel) { $peakVel = [Math]::Abs($vel[$i]); $peakAt = $i }
    }
    $peakFrac = ($peakAt - $swingFrom) / [double]$swingLen

    # Jerk is scanned over the swing only. The telegraph->swing handoff is checked separately below,
    # because a discontinuity exactly at that boundary is the specific defect worth naming.
    $jerkSpikes = 0
    $maxJerk = 0.0
    for ($i = [Math]::Max(2, $swingFrom + 1); $i -le $swingTo; $i++) {
        $j = [Math]::Abs($acc[$i])
        if ($j -gt $maxJerk) { $maxJerk = $j }
        if ($j -gt 8.0) { $jerkSpikes++ }
    }

    # Follow-through: settled frames immediately AFTER the swing ends. Scanning back from the very
    # last frame instead measures the tail of the ease-to-carry, which is a different thing.
    $endHold = 0
    for ($i = $swingTo + 1; $i -lt $n; $i++) {
        if ([Math]::Abs($vel[$i]) -lt 0.5) { $endHold++ } else { break }
    }

    # The snap this whole exercise started from: how far the weapon jumps on the first swing frame.
    $handoffSnap = 0.0
    if ($swingFrom -ge 1) { $handoffSnap = [Math]::Abs($vel[$swingFrom]) }

    # Telegraph and recovery get the same treatment as the swing. A wind-up that travels barely any
    # angle is not readable as a tell, and one that whips into place is not reactable; a recovery
    # that snaps back to the carry pose is the follow-through being thrown away.
    $windupSweep = 0.0
    $windupPeak = 0.0
    for ($i = 1; $i -lt $swingFrom; $i++) {
        $windupSweep += [Math]::Abs($vel[$i])
        if ([Math]::Abs($vel[$i]) -gt $windupPeak) { $windupPeak = [Math]::Abs($vel[$i]) }
    }

    $recoverySweep = 0.0
    $recoveryPeak = 0.0
    for ($i = $swingTo + 2; $i -lt $n; $i++) {
        $recoverySweep += [Math]::Abs($vel[$i])
        if ([Math]::Abs($vel[$i]) -gt $recoveryPeak) { $recoveryPeak = [Math]::Abs($vel[$i]) }
    }

    $activeCountForFrac = $activeCount
    if ($activeCountForFrac -eq 0) { $activeCountForFrac = $swingLen }
    $activeFrac = $activeCountForFrac / [double]$n

    $maxArmErr = ($f | Measure-Object -Property armWeaponErrorDeg -Maximum).Maximum
    $dirFlips = 0
    for ($i = 1; $i -lt $n; $i++) { if ($f[$i].dir -ne $f[$i - 1].dir) { $dirFlips++ } }

    Write-Host ''
    Write-Host "=== run $runId  $puppetName / $attackName ===" -ForegroundColor Cyan
    Write-Host ('  frames            {0}  ({1} ms @60fps)' -f $n, (Get-Ms $n))
    $swingTicks = $swingTo - $swingFrom + 1
    Write-Host ('  windup/swing/rec  {0} / {1} / {2} ticks   ({3} / {4} / {5} ms)' -f $windup, $swingTicks, $recovery, (Get-Ms $windup), (Get-Ms $swingTicks), (Get-Ms $recovery))
    Write-Host ('  damage window     {0} ticks ({1} ms) of the swing' -f $activeCount, (Get-Ms $activeCount))
    Write-Host ('  active fraction   {0:P0}   [Souls melee ~8-20%]  {1}' -f $activeFrac, (Get-Band ($activeFrac * 100) 8 20 'SHORT - hard to land' 'ok' 'LONG - reads as a lingering hitbox'))
    Write-Host ('  sweep net/total   {0:N0} / {1:N0} deg   [greatsword overhead ~150-200 net]  {2}' -f $sweepNet, $sweepTotal, (Get-Band $sweepNet 120 220 'SHALLOW - reads as a poke' 'ok' 'WIDE - may over-rotate'))
    Write-Host ('  peak ang. vel     {0:N1} deg/tick at t={1:P0}   [heavy peaks late ~0.5-0.7]  {2}' -f $peakVel, $peakFrac, (Get-Band $peakFrac 0.35 0.75 'EARLY - reads light/snappy' 'ok' 'LATE - reads sluggish'))
    Write-Host ('  max jerk/spikes   {0:N1} deg/tick^2, {1} spike(s) >8   [spikes are visible snapping]  {2}' -f $maxJerk, $jerkSpikes, (Get-Band $jerkSpikes -1 0 '' 'clean' 'SNAPS - discontinuous rotation'))
    Write-Host ('  end hold          {0} ticks parked after the swing   [0 = follow-through cut off]  {1}' -f $endHold, (Get-Band $endHold 1 9999 'NO SETTLE - swing stops dead' 'ok' 'ok'))
    Write-Host ('  telegraph motion  {0:N0} deg travelled, peak {1:N1} deg/tick   [flat = no readable tell]  {2}' -f $windupSweep, $windupPeak, (Get-Band $windupSweep 25 9999 'FLAT - wind-up barely moves, hard to read' 'ok' 'ok'))
    Write-Host ('  telegraph handoff {0:N1} deg jump on the first swing frame   [>6 = visible snap]  {1}' -f $handoffSnap, (Get-Band $handoffSnap -1 6 '' 'continuous' 'SNAP - wind-up does not reach the arc start'))
    Write-Host ('  recovery motion   {0:N0} deg travelled, peak {1:N1} deg/tick   [fast = follow-through thrown away]  {2}' -f $recoverySweep, $recoveryPeak, (Get-Band $recoveryPeak -1 6 '' 'settles gently' 'ABRUPT - snaps back to the carry pose'))
    Write-Host ('  facing flips      {0}   max arm/weapon error {1:N1} deg' -f $dirFlips, $maxArmErr)

    $W = 1120
    $H = 760
    $bmp = New-Object System.Drawing.Bitmap($W, $H)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.Clear([System.Drawing.Color]::FromArgb(255, 22, 24, 30))

    $fontT = New-Object System.Drawing.Font('Consolas', 13, [System.Drawing.FontStyle]::Bold)
    $fontS = New-Object System.Drawing.Font('Consolas', 10)
    $white = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::White)
    $gray = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 150, 155, 165))
    $axis = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 80, 85, 95)), 1

    $g.DrawString("$puppetName  /  $attackName   (run $runId, $n frames)", $fontT, $white, 14, 10)

    $ax = 20
    $ay = 44
    $aw = 520
    $ah = 400
    $g.DrawString('A  blade arc, NPC-local   grey=windup  red=active hitbox  blue=recovery', $fontS, $gray, $ax, $ay - 2)
    $g.DrawRectangle($axis, $ax, $ay + 16, $aw, $ah)

    $maxR = 1.0
    foreach ($s in $f) {
        foreach ($p in @($s.hand, $s.visualTip)) {
            $r = [Math]::Sqrt([Math]::Pow($p[0] - $s.npc[0], 2) + [Math]::Pow($p[1] - $s.npc[1], 2))
            if ($r -gt $maxR) { $maxR = $r }
        }
    }
    $cx = $ax + $aw / 2
    $cy = $ay + 16 + $ah * 0.60
    $scale = ($ah * 0.40) / $maxR
    $g.DrawLine($axis, $cx - 8, $cy, $cx + 8, $cy)
    $g.DrawLine($axis, $cx, $cy - 8, $cx, $cy + 8)

    for ($i = 0; $i -lt $n; $i++) {
        $s = $f[$i]
        $hx = $cx + ($s.hand[0] - $s.npc[0]) * $scale
        $hy = $cy + ($s.hand[1] - $s.npc[1]) * $scale
        $tx = $cx + ($s.visualTip[0] - $s.npc[0]) * $scale
        $ty = $cy + ($s.visualTip[1] - $s.npc[1]) * $scale
        if ($s.bladeArmed) {
            $col = [System.Drawing.Color]::FromArgb(220, 235, 70, 70)
        }
        elseif ($lastActive -ge 0 -and $i -gt $lastActive) {
            $col = [System.Drawing.Color]::FromArgb(150, 90, 150, 240)
        }
        else {
            $col = [System.Drawing.Color]::FromArgb(120, 165, 170, 180)
        }
        $pen = New-Object System.Drawing.Pen $col, 1.6
        $g.DrawLine($pen, $hx, $hy, $tx, $ty)
        $pen.Dispose()
        $dot = New-Object System.Drawing.SolidBrush $col
        $g.FillEllipse($dot, $tx - 1.8, $ty - 1.8, 3.6, 3.6)
        $dot.Dispose()
    }

    Draw-Series $g $axis $fontS $gray 590 44 460 170 'B  blade angle (deg, unwrapped)' $ang ([System.Drawing.Color]::FromArgb(255, 120, 220, 160)) $firstActive $lastActive $n
    Draw-Series $g $axis $fontS $gray 590 264 460 170 'C  angular velocity (deg/tick)' $vel ([System.Drawing.Color]::FromArgb(255, 255, 190, 90)) $firstActive $lastActive $n
    Draw-Series $g $axis $fontS $gray 590 484 460 170 'D  angular accel - spikes = snapping' $acc ([System.Drawing.Color]::FromArgb(255, 235, 110, 235)) $firstActive $lastActive $n

    $lines = @(
        ('windup/swing/recovery  : {0} / {1} / {2} ticks' -f $windup, $swingTicks, $recovery),
        ('                         {0} / {1} / {2} ms' -f (Get-Ms $windup), (Get-Ms $swingTicks), (Get-Ms $recovery)),
        ('damage window          : {0} ticks' -f $activeCount),
        ('active fraction        : {0:P0}' -f $activeFrac),
        ('sweep net / total      : {0:N0} / {1:N0} deg' -f $sweepNet, $sweepTotal),
        ('peak vel               : {0:N1} deg/tick at t={1:P0}' -f $peakVel, $peakFrac),
        ('max jerk / spikes >8   : {0:N1} / {1}' -f $maxJerk, $jerkSpikes),
        ('end hold               : {0} ticks' -f $endHold),
        ('telegraph handoff      : {0:N1} deg jump' -f $handoffSnap),
        ('facing flips           : {0}' -f $dirFlips)
    )
    $yy = 470
    $g.DrawString('metrics', $fontS, $gray, 20, $yy - 2)
    foreach ($l in $lines) {
        $yy += 18
        $g.DrawString($l, $fontS, $white, 24, $yy)
    }

    $safeAttack = ($attackName -replace '[^A-Za-z0-9]', '')
    $safePuppet = ($puppetName -replace '[^A-Za-z0-9]', '')
    $outPng = Join-Path $OutDir ('swing-{0}-{1}-run{2}.png' -f $safePuppet, $safeAttack, $runId)
    $bmp.Save($outPng, [System.Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose()
    $bmp.Dispose()
    Write-Host "  chart -> $outPng" -ForegroundColor Green
}
