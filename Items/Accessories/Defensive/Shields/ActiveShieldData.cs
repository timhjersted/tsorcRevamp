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

        public ActiveShieldData(float baseCost, float damageFactor, float moveSpeedMult = 0.75f, int activeDefense = 0, float knockback = 5f, ShieldResource resource = ShieldResource.Stamina)
        {
            BaseCost = baseCost;
            DamageFactor = damageFactor;
            MoveSpeedMult = moveSpeedMult;
            ActiveDefense = activeDefense;
            Knockback = knockback;
            Resource = resource;
        }
    }
}
