---
name: enemy-upgrade-audit
description: Audit an enemy's combat and animation coverage, and find suitable reference enemies. Use for broad audits, redesigns, comparisons, or upgrade planning—not for isolated balance edits or direct bug fixes.
---

# Enemy upgrade audit

This repo's enemies were upgraded in waves, and no two are at the same level. Use this audit to
establish an enemy's actual coverage before a broad combat/animation review, a redesign, or a
comparison. Do not infer that coverage from `git log` (most files share bulk commit dates) or memory.

## When the matrix is required

Regenerate and read the matrix when the work needs a coverage assessment—for example, when deciding
what an enemy is missing, selecting a reference enemy, planning multiple upgrades, or reviewing a
whole moveset. It is not required for a narrow, already-specified change such as a cooldown, damage,
drop rate, duration, or a direct bug fix. For those edits, inspect the affected source and callers
directly, make the requested change, and verify it proportionately.

## Broad audit workflow

```bash
bash .agents/tools/enemy-upgrade-matrix.sh --write
```

Then read [EnemyUpgradeMatrix.md](EnemyUpgradeMatrix.md). It has
the per-enemy coverage table, what each axis does, the ranked list of who to copy from, and the
cheapest-first upgrade ladder.

For non-puppet enemies (FighterAI/ArcherAI), use the equivalent checklist for poise, hyper-armor,
evasion, and kiting in [EnemyAbilityPortingGuide.md](EnemyAbilityPortingGuide.md).

## Do not substitute a grep for a broad audit

Several axes resolve from archetype defaults rather than the file, and an explicit `=> false` opt-out
greps identically to an opt-in. The generator resolves both; a grep does not. Specifically:

- `UseAimCenteredSwing` and `UseAuthoredComboSwingClock` default **on** for `WeaponArchetype.Axe`.
- `Gwyn` and `OwlFatherInvader` set `UseAlternateFlip => false` deliberately.
- Puppets absent from `PopulatePoiseProfiles()` still get a poise fallback in `GlobalNPC.SetDefaults`.

## When you add a new axis

If you add a new opt-in virtual to `PuppetNPC` that gates combat or animation quality, add it to the
`AXES` array in `.agents/tools/enemy-upgrade-matrix.sh` and to the legend table in the doc, in
the same commit. An axis that isn't in the matrix is one that will silently go un-adopted.

## Scope discipline

Reporting that an enemy is missing eight things is not permission to add eight things. Follow the
ladder in order, and confirm scope with the user before going past what they asked for — a puppet
change usually touches shared `PuppetNPC` code that every other boss inherits.
