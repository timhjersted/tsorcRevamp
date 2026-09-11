<#
Points .claude\skills at .agents\skills so Claude Code can discover the skills, which live in one
AI-agnostic folder shared with ChatGPT/Codex.

Why a junction rather than something git tracks: git has no object type for an NTFS junction, and a
symlink -- which git CAN store -- only materialises on Windows with core.symlinks plus Developer
Mode. Without those git writes a plain TEXT FILE holding the target path, so the skills directory
silently becomes an unreadable file. A local junction plus this script fails loudly instead.

Safe to re-run. Does nothing if the junction is already correct; refuses to touch a real directory.
#>

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$target   = Join-Path $repoRoot '.agents\skills'
$link     = Join-Path $repoRoot '.claude\skills'

if (-not (Test-Path $target))
{
    Write-Error "No skills folder at $target - nothing to link to."
}

$existing = Get-Item $link -ErrorAction SilentlyContinue

if ($existing)
{
    # A real directory here means someone made a second copy. Refuse rather than delete it: the
    # copies have almost certainly diverged and picking a winner is not this script's call.
    if ($existing.LinkType -ne 'Junction')
    {
        Write-Error "$link is a real directory, not a junction. Merge its contents into $target by hand, delete it, then re-run."
    }

    if ($existing.Target -contains $target)
    {
        Write-Host "Already linked: .claude\skills -> .agents\skills"
        exit 0
    }

    Write-Host "Re-pointing a stale junction (was $($existing.Target))"
    Remove-Item $link -Force
}

$claudeDir = Join-Path $repoRoot '.claude'

if (-not (Test-Path $claudeDir))
{
    New-Item -ItemType Directory -Path $claudeDir | Out-Null
}

New-Item -ItemType Junction -Path $link -Target $target | Out-Null

$count = (Get-ChildItem $link -Directory).Count
Write-Host "Linked .claude\skills -> .agents\skills ($count skills visible)"
