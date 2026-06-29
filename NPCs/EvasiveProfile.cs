using Microsoft.Xna.Framework;

namespace tsorcRevamp.NPCs
{
    /// <summary>
    /// Which kind of hit a given evasive behavior reacts to. Baked into each behavior at the point it is added to
    /// the selection pool in <see cref="tsorcRevampAIs.EvasiveOnHit"/> — it is a property of the BEHAVIOR, not a
    /// per-enemy config (e.g. "leap forward to close the gap" is a natural counter to ranged harassment, so it is
    /// <see cref="Ranged"/>; "retreat then shoot" answers being meleed, so it is <see cref="Melee"/>).
    /// </summary>
    public enum EvasiveHitSource
    {
        Melee,
        Ranged,
        Both
    }

    /// <summary>
    /// The individual on-hit evasive reactions. Each is gated by a matching capability flag on
    /// <see cref="tsorcRevampGlobalNPC"/> (e.g. <c>EvasiveRetreatJump</c>); an enemy opts into any subset and the
    /// shared <see cref="tsorcRevampAIs.EvasiveOnHit"/> selector builds a weighted pool from the enabled ones and
    /// samples it. Some are instantaneous (a one-shot velocity kick); the sustained ones (RunningDash /
    /// RetreatAndShoot / QuickStep) instead ARM a window that the AI-tick driver (UpdateEvasion / the QuickStep exec
    /// block) runs over several frames — see <see cref="tsorcRevampGlobalNPC.InEvasion"/>.
    /// </summary>
    public enum EvasiveBehavior
    {
        RetreatJump,     // hop / leap / high-arc drift away from the player (3 cosmetic variants) — instant
        RetreatDash,     // low, fast dash away — instant
        TeleportAway,    // blink away if CanTeleport, else fall back to a leap — instant
        LeapForward,     // lunge TOWARD the player to close the gap (counter to ranged harassment) — instant
        RunningDash,     // flash telegraph, then a grounded ~2s top-speed burst toward the player (hyper-armor) — sustained
        RetreatAndShoot, // back off (forced flee) for ~1-1.5s, then resume firing at range — sustained
        QuickStep,       // standing i-frame dash step (no rotation, can pass through the player) — sustained
        BasiliskWalkerCloseBackhop,     // close-hit basilisk walker backhop; keeps its old proximity gate
        BasiliskWalkerFarScrambleHop,   // far-hit basilisk walker scramble hop; keeps its old proximity gate
        BasiliskShifterCloseBackhop,    // close-hit basilisk shifter backhop; keeps its old proximity gate
        BasiliskShifterFarForwardHop    // far-hit basilisk shifter forward hop; keeps its old proximity gate
    }

    /// <summary>
    /// Named bundles of evasion capability flags, applied from an enemy's SetDefaults. A "profile" is purely a
    /// documented set of flag assignments — there is no dispatch on the profile name anywhere; the flags are the
    /// single source of truth. Convention: one method per enemy/family that shares a bundle (singular + enemy name,
    /// e.g. <see cref="RedKnight"/>); truly one-off enemies just set their flags inline instead.
    /// </summary>
    public static class EvasiveProfile
    {
        /// <summary>
        /// The Red Knight family's slippery back-off: hop/leap/dash away on hit, or blink away when able. Shared by
        /// the Red/Black Knight enemies and their Great/Test variants. Reproduces the original FighterEvasiveOnHit
        /// behavior exactly (TeleportAway falls back to a leap if the enemy lacks CanTeleport).
        /// </summary>
        public static void RedKnight(tsorcRevampGlobalNPC globalNPC)
        {
            globalNPC.EvasiveRetreatJump = true;
            globalNPC.EvasiveRetreatDash = true;
            globalNPC.EvasiveTeleportAway = true;
        }

        /// <summary>
        /// The Lothric knight family (Knight / Black Knight). Grounded, shield-and-blade heavies — NOT skittish
        /// dodgers, so no hop-away / blink / quick-step. Instead they answer being poked in neutral with aggressive
        /// gap-closers: a one-shot <see cref="EvasiveBehavior.LeapForward"/> to punish ranged kiting (they have no
        /// teleport, so this plugs the anti-kite hole), a telegraphed hyper-armored <see cref="EvasiveBehavior.RunningDash"/>
        /// charge, and an occasional low <see cref="EvasiveBehavior.RetreatDash"/> to reset spacing. All three are
        /// driven through the normal pursuit mover, so the knight flows straight into a slash/poke on arrival.
        /// </summary>
        public static void LothricKnight(tsorcRevampGlobalNPC globalNPC)
        {
            globalNPC.EvasiveLeapForward = true;
            globalNPC.EvasiveRunningDash = true;
            globalNPC.EvasiveRetreatDash = true;
        }

        /// <summary>
        /// The three invisible Dworc casters (Abysswalker, Alchemist, VoodooShaman). When meleed they back off and
        /// resume casting; they also i-frame quick-step out of incoming fire. Pairs naturally with
        /// <see cref="InvisibilityStyle.Evasive"/>, set on the enemy itself.
        /// </summary>
        public static void DworcShaman(tsorcRevampGlobalNPC globalNPC)
        {
            globalNPC.EvasiveRetreatAndShoot = true;
            globalNPC.EvasiveQuickStep = true;
            // Blink away on hit (in a grey smoke puff). TeleportAway falls back to a leap if CanTeleport isn't set,
            // but the shamans pass canTeleport:true to FighterAI so they actually blink.
            globalNPC.EvasiveTeleportAway = true;
            globalNPC.TeleportVisualStyle = TeleportVisualStyle.GreySmoke;
            // Smart positioning: hold a CLOSEISH 6-12 tile band — close enough to land the magic ring, far enough to
            // keep casting. Backs off below 6, lets pursuit close above 12, with looseness so melee can sometimes rush.
            globalNPC.KiteRangeMin = 6f;
            globalNPC.KiteRangeMax = 12f;
        }

        /// <summary>
        /// The lone ranged Dworc (Venomsniper): kite away when meleed, and quick-step through incoming projectiles
        /// to keep its high-ground line. One enemy uses this — its own profile, per convention.
        /// </summary>
        public static void DworcSniper(tsorcRevampGlobalNPC globalNPC)
        {
            globalNPC.EvasiveRetreatAndShoot = true;
            globalNPC.EvasiveQuickStep = true;
            // Smart positioning: a sniper wants LONG range — hold a 10-25 tile band (only on the same level; its
            // high-ground perches are handled by the standoff). Backs off hard if you close inside 10.
            globalNPC.KiteRangeMin = 10f;
            globalNPC.KiteRangeMax = 25f;
        }

        /// <summary>
        /// The basilisk family (Walker / Shifter / Hunter): reptilian spitters that hop and dash. On hit they hop or
        /// dash away, or i-frame quick-step through it. Teleporters (e.g. Walker's limited blinks) can also set
        /// <c>EvasiveTeleportAway</c> on top.
        /// </summary>
        public static void Basilisk(tsorcRevampGlobalNPC globalNPC)
        {
            globalNPC.EvasiveRetreatJump = true;
            globalNPC.EvasiveRetreatDash = true;
            globalNPC.EvasiveQuickStep = true;
        }

        public static void BasiliskHunterAttackJumps(tsorcRevampGlobalNPC globalNPC)
        {
            globalNPC.CanJumpBeforeAttack = true;
            globalNPC.JumpBeforeAttackChance = 0.4f;
            globalNPC.JumpBeforeAttackDesperateLifeFraction = 0.2f;
            globalNPC.JumpBeforeAttackRequiresGrounded = false;
            globalNPC.JumpBeforeAttackShortForwardHop = true;
            globalNPC.JumpBeforeAttackHighForwardHop = true;
            globalNPC.JumpBeforeAttackOffensiveLeap = true;
            globalNPC.JumpBeforeAttackDashHop = true;
            globalNPC.JumpBeforeAttackDesperateHighHop = true;
            globalNPC.JumpBeforeAttackDesperateSprayHop = true;
        }

        public static void BasiliskShifterAttackJumps(tsorcRevampGlobalNPC globalNPC)
        {
            globalNPC.CanJumpBeforeAttack = true;
            globalNPC.JumpBeforeAttackChance = 0.4f;
            globalNPC.JumpBeforeAttackDesperateLifeFraction = 0.5f;
            globalNPC.JumpBeforeAttackRequiresGrounded = false;
            globalNPC.JumpBeforeAttackMainAttackHop = true;
            globalNPC.JumpBeforeAttackErraticFinalHover = true;
        }

        /// <summary>
        /// The Omnir giant beasts (Massacre / Hydra) are huge, slow, grounded
        /// quadrupeds or 2 legged, tall giants (Gigas / Ice Gigas / Demon Lord Apocalypse) — NOT skittish dodgers, so no hop-away / blink / weightless quick-step (those read wrong at this
        /// scale). When struck in neutral they answer with weighty spacing plays: a low <see cref="EvasiveBehavior.RetreatDash"/>
        /// to reset distance (the "lumber backwards when it can't reach you" beat), or a telegraphed hyper-armored
        /// <see cref="EvasiveBehavior.RunningDash"/> charge straight back in. Both flow through the normal SF4 pursuit
        /// mover. The dash multiplier is cranked because beast top speeds are tiny (~0.5), so the burst needs a big
        /// multiplier to actually read as a charge.
        /// </summary>
        public static void HeavyBeast(tsorcRevampGlobalNPC globalNPC)
        {
            globalNPC.EvasiveRetreatDash = true;
            globalNPC.EvasiveRunningDash = true;
            globalNPC.EvasiveDashSpeedMult = 6f; // This may be too fast. Need to test
        }

        /// <summary>
        /// The Cursed Dragon invader: a winged spear-and-spellcaster bruiser. Not a skittish dodger, so no
        /// hop-away / blink / weightless quick-step (those read wrong on a heavy armored dragon). When struck in
        /// neutral it answers with aggressive spacing plays — a one-shot <see cref="EvasiveBehavior.LeapForward"/>
        /// to punish ranged kiting (it has no teleport), a telegraphed hyper-armored <see cref="EvasiveBehavior.RunningDash"/>
        /// charge straight back in, and an occasional low <see cref="EvasiveBehavior.RetreatDash"/> to reset to
        /// meteor / arcane-ball range. All three flow through the SF4 pursuit mover, so it slides straight into a
        /// spear poke or combo on arrival. (Same bundle as <see cref="LothricKnight"/>, named per-enemy by convention.)
        /// </summary>
        public static void CursedDragon(tsorcRevampGlobalNPC globalNPC)
        {
            globalNPC.EvasiveLeapForward = true;
            globalNPC.EvasiveRunningDash = true;
            globalNPC.EvasiveRetreatDash = true;
            // On-hit i-frame quick-step.  SAFE on invaders now that InvaderNPC ticks the shared executor
            // (tsorcRevampAIs.TickQuickStep) every frame — previously QuickStepTimer only advanced inside the
            // FighterAI combat layer, so on an invader it would have stuck on → permanent i-frames.
            globalNPC.EvasiveQuickStep = true;
        }

        /// <summary>
        /// The AddAttack ghost fighters (Forgotten Warrior / Forgotten Knight / Darkmoon Knight): phase back or
        /// i-frame quick-step when hit. No teleport flag — they don't set CanTeleport (they have their own ghost
        /// wall-phase), so TeleportAway would just leap. QuickStep's i-frame dodge-through fits a phasing ghost.
        /// </summary>
        public static void Ghost(tsorcRevampGlobalNPC globalNPC)
        {
            globalNPC.EvasiveRetreatJump = true;
            globalNPC.EvasiveRetreatDash = true;
            globalNPC.EvasiveQuickStep = true;
        }

        /// <summary>
        /// Configures the threat-reactive <see cref="InvisibilityStyle.Evasive"/> cloak (the AI-tick invisibility
        /// capability — separate from the on-hit evasion flags above). A hit or a nearby threat rolls
        /// <paramref name="cloakChance"/> to cloak. Sneakier/stronger enemies pass a higher chance (~0.4-0.5); weaker
        /// ones ~0.1-0.2. Cloak DURATION and post-reveal cooldown both scale with the chance and are set EQUAL (3s at
        /// 0.1 → 9s at 0.5): a strong cloaker vanishes for a long stretch then is exposed for just as long, a weak one
        /// flickers out briefly and often — both ~50% uptime. An ordinary hit can end a cloak early, but higher-tier
        /// cloakers resist that (reveal chance scales DOWN with cloakChance, ~0.9 → ~0.35); a poise-break stagger is
        /// always a guaranteed reveal. Pass <paramref name="threatRange"/> > 0 so it also cloaks when the player closes in.
        /// </summary>
        public static void EvasiveCloak(tsorcRevampGlobalNPC globalNPC, float cloakChance, int threatRange = 0)
        {
            globalNPC.InvisibilityStyle = InvisibilityStyle.Evasive;
            globalNPC.EvasiveCloakChance = cloakChance;
            globalNPC.EvasiveThreatRange = threatRange;
            float tier = MathHelper.Clamp((cloakChance - 0.1f) / 0.4f, 0f, 1f); // 0 at chance 0.1, 1 at chance 0.5
            // Duration == cooldown, both scaling with how easily it cloaks: 3s → 9s.
            int ticks = (int)MathHelper.Lerp(180f, 540f, tier);
            globalNPC.EvasiveCloakDurationTicks = ticks;
            globalNPC.EvasiveCloakCooldownTicks = ticks;
            // Harder to shoot a high-tier cloaker out of its cloak (stagger still bypasses this entirely).
            globalNPC.InvisibleHitRevealChance = MathHelper.Lerp(0.9f, 0.35f, tier);
        }
    }
}
