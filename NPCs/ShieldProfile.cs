namespace tsorcRevamp.NPCs
{
    /// <summary>
    /// Named bundles of reactive-shield capability flags (the <c>Shield*</c> / <c>*Block*</c> fields on
    /// <see cref="tsorcRevampGlobalNPC"/>), applied from a shield enemy's SetDefaults. Like
    /// <see cref="EvasiveProfile"/>, a profile is just a documented set of flag assignments — the flags are the single
    /// source of truth, there is no dispatch on the profile name. The enemy's AUTONOMOUS shield-timer rhythm stays in
    /// its own file; this only layers on the reactive pre-emptive / on-hit block.
    ///
    /// Wiring an enemy takes three touches (see HollowSoldier / LothricKnight): a profile call in SetDefaults,
    /// <c>tsorcRevampAIs.TryPreemptiveBlock(NPC, globalNPC)</c> in its neutral AI, and
    /// <c>tsorcRevampAIs.TryOnHitBlock(NPC, globalNPC, melee)</c> in its OnHitBy* hooks, then OR
    /// <c>globalNPC.ReactiveBlockTimer &gt; 0</c> into its <c>shielding</c> state.
    /// </summary>
    public static class ShieldProfile
    {
        /// <summary>
        /// Hollow-tier shielders (Hollow Soldier / Hollow Spearman): occasional reactive guard. Weaker
        /// reflexes than the elite knights — lower block chances, shorter threat reach.
        /// </summary>
        public static void Hollow(tsorcRevampGlobalNPC globalNPC)
        {
            globalNPC.PreemptiveBlockChance = 0.02f;  // per-tick roll while a threat is detected
            globalNPC.OnHitBlockChance = 0.25f;
            globalNPC.ShieldThreatRange = 80;         // ~5 tiles — only guards on a close melee approach
            globalNPC.ShieldedWalkSpeed = 0f;         // plant while guarding
        }

        /// <summary>
        /// Lothric-tier shielders (Knight / Spear Knight / Black Knight): disciplined guard — more reliable reactive
        /// blocks and longer threat reach.
        /// </summary>
        public static void LothricKnight(tsorcRevampGlobalNPC globalNPC)
        {
            globalNPC.PreemptiveBlockChance = 0.04f;
            globalNPC.OnHitBlockChance = 0.45f;
            globalNPC.ShieldThreatRange = 96;         // ~6 tiles
            globalNPC.ShieldedWalkSpeed = 0f;         // plant while guarding
        }
    }
}
