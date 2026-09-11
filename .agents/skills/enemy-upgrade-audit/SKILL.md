---
name: enemy-upgrade-audit
description: Check which combat/animation improvements an enemy already has before working on it, and find the right enemy to copy from. Use when starting work on any NPC's melee, swings, animation, poise, or evasion — or when asked "what is X missing", "which enemies have Y", "is X up to date", "who should I copy from", or to compare two enemies' capabilities.
---

# Enemy upgrade audit

This repo's enemies were upgraded in waves, and no two are at the same level. Before touching an
enemy's combat code, establish where it actually sits — do not infer it from `git log` (most of these
files share bulk commit dates, so recency tells you nothing about maturity) and do not infer it from
memory of past sessions.

## 1. Regenerate the matrix, then read it

```bash
bash .agents/tools/enemy-upgrade-matrix.sh --write
```

Then read [EnemyUpgradeMatrix.md](EnemyUpgradeMatrix.md). It has
the per-enemy coverage table, what each axis does, the ranked list of who to copy from, and the
cheapest-first upgrade ladder.

For non-puppet enemies (FighterAI/ArcherAI) the equivalent checklist — poise, hyper-armor windows,
evasion, kiting — is [EnemyAbilityPortingGuide.md](EnemyAbilityPortingGuide.md).

## 2. Never audit this with a bare grep

Several axes resolve from archetype defaults rather than the file, and an explicit `=> false` opt-out
greps identically to an opt-in. The generator resolves both; a grep does not. Specifically:

- `UseAimCenteredSwing` and `UseAuthoredComboSwingClock` default **on** for `WeaponArchetype.Axe`.
- `Gwyn` and `OwlFatherInvader` set `UseAlternateFlip => false` deliberately.
- Puppets absent from `PopulatePoiseProfiles()` still get a poise fallback in `GlobalNPC.SetDefaults`.

## 3. When you add a new axis

If you add a new opt-in virtual to `PuppetNPC` that gates combat or animation quality, add it to the
`AXES` array in `.agents/tools/enemy-upgrade-matrix.sh` and to the legend table in the doc, in
the same commit. An axis that isn't in the matrix is one that will silently go un-adopted.

## 4. Scope discipline

Reporting that an enemy is missing eight things is not permission to add eight things. Follow the
ladder in order, and confirm scope with the user before going past what they asked for — a puppet
change usually touches shared `PuppetNPC` code that every other boss inherits.
