#!/usr/bin/env bash
#
# Regenerates the coverage table in .agents/skills/enemy-upgrade-audit/EnemyUpgradeMatrix.md by reading the source,
# so it can never drift from the code the way a hand-kept list does.
#
#   bash .agents/tools/enemy-upgrade-matrix.sh          # print to stdout
#   bash .agents/tools/enemy-upgrade-matrix.sh --write   # splice into the doc
#
# Reads each PuppetNPC subclass for the opt-in virtuals that gate melee/animation quality.
# DEFAULTS MATTER: several axes are on by default for some archetypes, so a bare "does the file
# mention this identifier" grep both over- and under-reports. This resolves the actual value.

set -u
cd "$(dirname "${BASH_SOURCE[0]}")/../.." || exit 1

DOC=".agents/skills/enemy-upgrade-audit/EnemyUpgradeMatrix.md"

# Axis list: <identifier>|<short column header>
AXES=(
    "UseCompositeArmSwing|arm"
    "UseTwoHandedCompositeSwing|2hnd"
    "UseSwingEasing|ease"
    "UseAuthoredComboSwingClock|clock"
    "UseLogicalMeleeTelegraphs|tele"
    "MirrorMeleeSwingRotationByFacing|mirr"
    "UseAimCenteredSwing|aimC"
    "UseAimAdaptiveArc|aimA"
    "UseAlternateFlip|flip"
    "ModifyMeleeArcEndpoints|arcs"
    "OverheadWindupOvershoot|over"
    "MeleeComboInterStepLingerTicks|link"
    "MeleeRecoveryLingerTicks|foll"
    "UseLandingTimedLeapSlam|land"
    "MeleeComboPoolOverride|pool"
    "RuntimeV2Clip|clip"
)

# Resolve one axis for one file to a cell string.
#   Y   explicitly opted in
#   n   explicitly opted OUT (a deliberate decision, not an oversight)
#   Y*  on via an archetype default, not written in the file
#   .   off, inherited default
cell() {
    local file="$1" axis="$2" archetype="$3"
    local line value

    line=$(grep -oE "override[^=;{]*\b${axis}\b[^=]*=>[^;]*" "$file" | head -1)

    if [ -z "$line" ]; then
        # No override. Fall back to the base-class default for this archetype.
        case "$axis" in
            UseAimCenteredSwing|UseAuthoredComboSwingClock)
                # PuppetNPC: UseAimCenteredSwing defaults true for Axe;
                # UseAuthoredComboSwingClock defaults to AimSwingActive.
                [ "$archetype" = "Axe" ] && { printf 'Y*'; return; }
                ;;
        esac
        # Methods and table hooks have no "=>" form when overridden as a body.
        if grep -qE "override[^=;{]*\b${axis}\b" "$file"; then printf 'Y'; return; fi
        # RuntimeV2Clip is an initialiser on a combo, not an override.
        if [ "$axis" = "RuntimeV2Clip" ] && grep -q "RuntimeV2Clip" "$file"; then printf 'Y'; return; fi
        printf '.'
        return
    fi

    value=$(printf '%s' "$line" | sed 's/.*=>//' | tr -d ' ')
    case "$value" in
        false)      printf 'n' ;;
        true)       printf 'Y' ;;
        0|0f|0.0f)  printf 'n' ;;
        *)          printf 'Y' ;;
    esac
}

emit_table() {
    local header sep f name archetype

    header="| Enemy | archetype |"
    sep="|---|---|"
    for entry in "${AXES[@]}"; do
        header="$header ${entry#*|} |"
        sep="$sep:-:|"
    done
    printf '%s\n%s\n' "$header" "$sep"

    for f in $(grep -rl ": PuppetNPC" --include=*.cs . | sort); do
        name=$(basename "$f" .cs)
        archetype=$(grep -oE "MeleeArchetype *=> *WeaponArchetype\.[A-Za-z]+" "$f" | head -1 | sed 's/.*\.//')
        [ -z "$archetype" ] && archetype="—"

        printf '| %s | %s |' "$name" "$archetype"
        for entry in "${AXES[@]}"; do
            printf ' %s |' "$(cell "$f" "${entry%%|*}" "$archetype")"
        done
        printf '\n'
    done
}

if [ "${1:-}" = "--write" ]; then
    if [ ! -f "$DOC" ]; then echo "missing $DOC" >&2; exit 1; fi
    awk -v tbl="$(emit_table)" '
        /^<!-- GENERATED:BEGIN -->$/ { print; print ""; print tbl; print ""; skip=1; next }
        /^<!-- GENERATED:END -->$/   { skip=0 }
        !skip { print }
    ' "$DOC" > "$DOC.tmp" && mv "$DOC.tmp" "$DOC"
    echo "updated $DOC"
else
    emit_table
fi
