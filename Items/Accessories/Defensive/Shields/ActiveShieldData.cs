namespace tsorcRevamp
{
    /// <summary>Which resource a shield spends to block, under the Active Shields Revamp.</summary>
    public enum ShieldResource
    {
        Stamina,
        Mana
    }

    /// <summary>
    /// Balance data for a single shield under the Active Shields Revamp.
    /// Stamina/mana cost to block a hit = ceil(BaseCost + incomingDamage * DamageFactor).
    /// Lower BaseCost and DamageFactor = a better shield ("stability").
    /// The registry mapping item type -> ActiveShieldData lives in tsorcRevamp.ActiveShieldRegistry,
    /// populated in tsorcRevamp.PopulateArrays().
    /// </summary>
    public readonly struct ActiveShieldData
    {
        public readonly float BaseCost;
        public readonly float DamageFactor;
        public readonly ShieldResource Resource;
        /// <summary>Fraction of move speed retained while this shield is raised (1 = no slow, 0.6 = 40% slower).
        /// Better shields slow less; heavy starters slow more.</summary>
        public readonly float MoveSpeedMult;
        /// <summary>Flat defense this shield grants in active mode (replaces its old, larger passive defense).
        /// Scales 2 (first) → 15 (final) by progression; 0 for the mana wards.</summary>
        public readonly int ActiveDefense;
        /// <summary>On-block knockback strength (px/frame applied to the attacker, before resist scaling + clamp).
        /// Better shields shove a bit harder; the floor/cap keep even the strongest to a few tiles.</summary>
        public readonly float Knockback;

        /// <summary>
        /// Fraction of a blocked hit's RAW damage that leaks through as unblockable chip damage on an ordinary
        /// (non-perfect) block. -1 = auto-derive from DamageFactor (the default, see ResolvedChipFraction).
        ///
        /// Deliberately its own stat rather than a pure function of DamageFactor, so a shield can trade chip
        /// against a different cost entirely: a greatshield sets 0 (nothing gets through) and pays with a longer
        /// BlockRegenDelayMult + heavier MoveSpeedMult, while a buckler sets a high value and pays almost nothing
        /// else. Cost and leakiness are separate axes.
        /// </summary>
        public readonly float ChipFactor;

        /// <summary>Multiplier on the post-block stamina regen pause (see tsorcRevampStaminaPlayer.BlockRegenDelay).
        /// 1 = the standard doubled-on-block delay; >1 is the heavy-shield tax; &lt;1 is the buckler's quick recovery.</summary>
        public readonly float BlockRegenDelayMult;

        /// <summary>Always-on stamina regen penalty while this shield is equipped, as a fraction (0.05 = -5%).
        /// The greatshield "equip load" tax: unlike BlockRegenDelayMult it costs you even when you never raise it,
        /// so a no-chip greatshield is a genuine commitment rather than a strict upgrade. Applied only in active
        /// mode (config + SoulsMode), alongside ActiveDefense.</summary>
        public readonly float PassiveRegenPenalty;

        // Chip auto-derivation: maps shield quality (DamageFactor) onto a chip band. Anchored on the registry's
        // current best/worst physical factors, so the whole roster spans the full band.
        private const float ChipBestFactor = 0.14f;   // Icebound Mythril Aegis
        private const float ChipWorstFactor = 0.45f;  // Mana Shield
        private const float ChipBestFraction = 0.06f;
        private const float ChipWorstFraction = 0.15f;

        /// <summary>The chip fraction actually used: the explicit ChipFactor when one was set, otherwise derived
        /// from DamageFactor so any shield added to the registry gets a sane value for free.</summary>
        public float ResolvedChipFraction
        {
            get
            {
                if (ChipFactor >= 0f)
                {
                    return ChipFactor;
                }
                float t = (DamageFactor - ChipBestFactor) / (ChipWorstFactor - ChipBestFactor);
                t = t < 0f ? 0f : (t > 1f ? 1f : t);
                return ChipBestFraction + (ChipWorstFraction - ChipBestFraction) * t;
            }
        }

        /// <summary>Shield quality as 0 (best in the registry) → 1 (worst), from DamageFactor. Shared by the
        /// riposte poise scale and the block-sound pitch so "better shield" means one consistent thing.</summary>
        public float QualityT
        {
            get
            {
                float t = (DamageFactor - ChipBestFactor) / (ChipWorstFactor - ChipBestFactor);
                return t < 0f ? 0f : (t > 1f ? 1f : t);
            }
        }

        /// <summary>True for the greatshield archetype: takes no chip at all, and pays for it elsewhere.</summary>
        public bool IsGreatshield => ChipFactor == 0f;

        public ActiveShieldData(float baseCost, float damageFactor, float moveSpeedMult = 0.75f, int activeDefense = 0, float knockback = 5f, ShieldResource resource = ShieldResource.Stamina, float chipFactor = -1f, float blockRegenDelayMult = 1f, float passiveRegenPenalty = 0f)
        {
            BaseCost = baseCost;
            DamageFactor = damageFactor;
            MoveSpeedMult = moveSpeedMult;
            ActiveDefense = activeDefense;
            Knockback = knockback;
            Resource = resource;
            ChipFactor = chipFactor;
            BlockRegenDelayMult = blockRegenDelayMult;
            PassiveRegenPenalty = passiveRegenPenalty;
        }
    }
}
