using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items;
using tsorcRevamp.Content.Items.BossBags;
using tsorcRevamp.Content.Items.Potions;
using tsorcRevamp.Content.Items.Tools;
using tsorcRevamp.Content.Items.Weapons.Melee.Shortswords;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Bosses.Death
{
    /// <summary>
    /// Normal Death - the reworked fight.
    ///
    /// STRUCTURE: everything shared lives in DeathBossBase; this file only tunes the normal variant.
    ///   DeathBossBase.cs           - the whole state machine: phase one, the phase transition, the
    ///                                phase-two move sequence (shuffled bags), the containment ring,
    ///                                every attack pattern and every tunable knob.
    ///   Death.cs (this file)       - palette, damage numbers, phase-one pacing for normal Death.
    ///   Special/TrueDeath.cs       - Absolute Death: same machine, red palette, bigger numbers,
    ///                                tighter ring and a few move tweaks.
    ///   DeathVFX.cs                - shader aura helper (UnblockableGlow) for body/summon/sickle.
    ///   DeathContainmentRingVFX.cs - ring fog (GreatRedKnightDominion) and glowing edge (Artorias).
    ///   DeathReaper.cs             - the damageable summon (formations, lasers/sickles, Strong5 rush,
    ///                                Strong6 firing line).
    ///   DeathSickleWeapon.cs       - standalone held scythe NPC (owner-attached / end / middle pivot).
    ///   Content/Projectiles/Enemy/Death/* - bolts, small sickles, giant flaming scythe (+ phantom),
    ///                                laser warning line, reaper laser, spawn warning.
    ///
    /// SPRITES: Death.png / TrueDeath.png are 8 frames, 132 px wide, frame height = texture height / 8
    /// (currently 176). Frames 0-3 are the idle loop, 4-7 the swing loop (NormalAnimationFrameCount /
    /// SpecialAnimationFrameStart). The artwork was re-centred inside each frame by adding transparent
    /// padding on top, so drawing with frame.Size() / 2 lands the body on the hitbox centre - keep that
    /// padding if the sheet is ever re-exported. DeathOld.png / DeathOld2.png are unused legacy art.
    ///
    /// TUNING: every value is a virtual member on DeathBossBase; a variant only overrides what differs.
    ///   damage / health / defense - BoltDamage, SickleDamage, GiantScytheDamage, BodyContactDamage,
    ///                               the GetReaper* overrides and NPC.lifeMax in SetDefaults.
    ///   colours                   - AuraColor, AuraDustColor, DespawnTextColor and
    ///                               DeathBossBase.VfxGlowColor (drives body, summon and sickle aura).
    ///   aura shape / brightness   - DeathVFX.DrawShaderGlow parameters at the three call sites in
    ///                               DeathBossBase.DrawBodyGlow, DeathReaper.PreDraw and
    ///                               DeathSickleWeapon.PreDraw (haloRadius, bloomScale, rim/core
    ///                               opacity, coreWhiteMix, phase).
    ///   ring                      - ContainmentRingRadius, the ContainmentFog* colours,
    ///                               ContainmentFogOpacity, ContainmentRingExpandTicks.
    ///   attack timing             - the PhaseTwo* tick constants near the top of DeathBossBase; each
    ///                               move is a Start/Tick pair (StartPhaseTwo*Move / TickPhaseTwo*Move).
    ///
    /// ADDING A MOVE: add a PhaseTwoAttackState entry plus a Start/Tick pair, hook it into the
    /// shuffle-bag sequence (weak/strong bag masks), and reuse the existing helpers
    /// (DeathVFX.DrawShaderGlow, DeathLaserWarning.Spawn, DeathSpawnWarning.SpawnFlamingScythe,
    /// DeathSickleWeapon.Spawn, ClearDeathReapers).
    ///
    /// MAINTENANCE NOTES:
    ///   - DeathBossBase.PreDraw draws the body itself and returns false: the vanilla draw path landed
    ///     this NPC's sprite ~34 source px above its own hitbox/aura. Consequence: no vanilla hover
    ///     name/health text (the boss bar UI and mod-side bars are unaffected).
    ///   - The aura is shader-driven through Effects/UnblockableGlow, which is SHARED with the
    ///     unblockable telegraph glow - edit that .fx only with both call sites in mind.
    ///   - Custom state must be written in SendExtraAI and read in ReceiveExtraAI in the same order.
    ///   - The pre-rework AI (homing volley + warping) is kept commented out at the bottom of
    ///     DeathBossBase, with its overrides commented out here and in Special/TrueDeath.cs.
    ///   - The old soft blur masks (DeathBlur / DeathReaperBlur / DeathSickleWeaponBlur) and the
    ///     non-shader glow path were removed.
    /// </summary>
    [AutoloadBossHead]
    class Death : DeathBossBase
    {
        protected override Color AuraColor => new Color(105, 60, 180);
        protected override int PhaseOneBoltCount => 0;
        protected override Color PhaseOneBoltColor => new Color(165, 40, 255);
        protected override Color AuraDustColor => new Color(40, 8, 62);
        protected override Color DespawnTextColor => Color.DarkMagenta;
        protected override int DespawnDustType => DustID.Demonite;
        // OLD AI (see the commented block at the bottom of DeathBossBase). Restoring it also needs
        // `using tsorcRevamp.Content.Projectiles.Enemy;` back for ShadowShot.
        // protected override int VolleyProjectileType => ModContent.ProjectileType<ShadowShot>();
        // EnemyDamage.Projectile declares the Expert pre-defense hit; vanilla applies Normal/Master scaling.
        protected override int BoltDamage => EnemyDamage.Projectile(222);
        protected override int SickleDamage => EnemyDamage.Projectile(222);
        protected override int GiantScytheDamage => 222;
        protected override int BodyContactDamage => 222;
        // Reaper contact is declared as an Expert baseline for EnemyDamage.SetContact.
        protected override int GetReaperContactDamage() => 222;
        protected override float PhaseTwoStrong5CircleMoveMaxSpeed => 2f;
        protected override bool PhaseTwoStrong2BodyBoltsEnabled => false;

        public override void SetDefaults()
        {
            base.SetDefaults();

            NPC.damage = 222;
            NPC.defense = 66;
            NPC.scale = 1.1f;
            NPC.lifeMax = 33333;
        }

        public override void OnSpawn(IEntitySource source)
        {
            // Absolute Death is restricted to Super Hard Mode blood moons.
            if (!Main.bloodMoon || !tsorcRevampWorld.SuperHardMode)
            {
                return;
            }

            Vector2 spawnPosition = NPC.position;
            NPC.active = false;

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                NPC.NewNPC(source, (int)spawnPosition.X, (int)spawnPosition.Y, ModContent.NPCType<Special.TrueDeath>());
            }
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.BossBag(ModContent.ItemType<DeathBag>()));
            IItemDropRule notExpertCondition = new LeadingConditionRule(new Conditions.NotExpert());
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<HolyWarElixir>(), 1, 2, 4));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<GreatMagicShieldScroll>(), 6));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<MagicBarrierScroll>()));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<Laevateinn>()));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<Humanity>()));
            npcLoot.Add(notExpertCondition);
        }
    }
}
