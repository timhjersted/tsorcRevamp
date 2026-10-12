using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items;
using tsorcRevamp.Content.Items.BossBags;
using tsorcRevamp.Content.Items.ConsumableSoul;
using tsorcRevamp.Content.Items.Materials;
using tsorcRevamp.Content.Items.Materials.Souls;
using tsorcRevamp.Content.Items.Potions;
using tsorcRevamp.Content.Items.Tools;
using tsorcRevamp.Content.Items.Weapons.Melee.Shortswords;
using tsorcRevamp.Content.Projectiles.Enemy.Death;
using tsorcRevamp.NPCs.Bosses.Death;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Special
{
    /// <summary>
    /// Absolute Death: the Super Hard Mode blood-moon variant. Same state machine as Death; the red
    /// palette, higher damage/health and tightened containment ring come from the overrides below.
    /// </summary>
    [AutoloadBossHead]
    class TrueDeath : DeathBossBase
    {
        protected override Color AuraColor => new Color(175, 10, 20);
        protected override Color AuraDustColor => new Color(55, 8, 14);
        protected override float AuraDustScale => 0.5f;
        protected override Color DespawnTextColor => Color.Red;
        protected override int DespawnDustType => DustID.CrimsonSpray;
        // OLD AI (see the commented block at the bottom of DeathBossBase). Restoring these also
        // needs `using tsorcRevamp.Content.Projectiles.Enemy;` back for BloodShot.
        // protected override int VolleyProjectileType => ModContent.ProjectileType<BloodShot>();
        // protected override int VolleyInterval => 11;
        // protected override int VolleyCount => 7;
        // protected override float VolleySpeed => 0.8f;
        // protected override int NormalWarpTicks => 120;
        // protected override int LowHealthWarpTicks => 85;
        // protected override float LowHealthWarpThreshold => 0.25f;
        // Declared as the Expert baseline and converted for the spawn call (see EnemyDamage.Projectile):
        // 444 lands at 666 in master mode.
        protected override int BoltDamage => EnemyDamage.Projectile(444);
        protected override int SickleDamage => EnemyDamage.Projectile(444);
        protected override int GiantScytheDamage => 666;
        protected override int BodyContactDamage => 2222;
        protected override float PhaseDustHealthThreshold => 0.40f;
        protected override float HighHealthDustScale => 1.2f;
        protected override float LowHealthDustScale => 1.7f;
        protected override int PhaseOneSickleInterval => 20;
        protected override float ContainmentRingRadius => 900f;
        protected override int PhaseTransitionHealAmount => 6666;
        protected override int PhaseTwoWeakBoltMaxIntervalTicks => 20;
        protected override int PhaseTwoWeakBoltMinIntervalTicks => 7;
        protected override int PhaseOneSpecialDashGapTicks => 0;
        protected override int PhaseTwoFiveDashGapTicks => 0;
        protected override float PhaseOneSpecialDashSpeed => 30f;
        protected override float PhaseTwoFiveDashSpeed => PhaseOneSpecialDashSpeed * 1.2f;
        protected override int PhaseOneBoltCount => 7;
        protected override float PhaseOneBoltSpreadDegrees => 15f;
        protected override int PhaseTwoBoltCount => 3;
        protected override float PhaseTwoBoltSpreadDegrees => 15f;
        protected override float PhaseTwoBoltSpeed => 30f;
        protected override int PhaseTwoBodyBoltCount => 5;
        protected override float PhaseTwoBodyBoltSpacingDegrees => 45f;
        // Six reapers in the formation move.
        protected override int PhaseTwoReaperCount => 6;
        protected override int PhaseTwoReaperBurstShotCount => 3;
        protected override int PhaseTwoReaperOrbitVolleyIntervalTicks => 30;
        protected override bool PhaseTwoStrong2MovesOnCircle => true;
        protected override int PhaseTwoReaperLaserCount => 2;
        protected override int PhaseTwoReaperLaserIntervalTicks => 5;
        protected override int PhaseTwoWeak2ScytheCount => 12;
        protected override int PhaseTwoWeak3ReverseLineCount => 5;
        protected override int PhaseTwoWeak4VolleyCount => 4;
        protected override bool LargeScythePhantomsEnabled => false;
        protected override bool PhaseTwoWeak2FullCircle => true;
        protected override int PhaseTwoStrong5WaveCount => 8;
        protected override int PhaseTwoStrong5WaveIntervalTicks => 30;
        protected override int PhaseTwoScatterInnerStartTicks => 135;
        protected override bool PhaseTwoStrong2UsesSickles => true;
        protected override bool PhaseTwoScatterBodyBoltsEnabled => false;
        // Scatter move: Absolute Death does not throw flaming scythes itself, it keeps sliding onto
        // the circle around the player instead.
        protected override bool PhaseTwoScatterBodyScytheEnabled => false;
        protected override bool PhaseTwoScatterMovesOnCircle => true;
        protected override float GoreScale => 1.3f;
        internal override float SickleScaleMultiplier => 1.25f;
        // Reaper health is applied as base * difficulty (x2 expert, x3 master), so 22222 shows up as
        // 66666 in master mode.
        protected override int GetReaperLifeMax() => 22222;
        protected override int GetReaperDefense() => 66;
        protected override int GetReaperContactDamage() => 2222;
        internal override bool ReaperStrong5WarningEnabled => false;
        // Absolute Death drops the five-dash (move 2); the scatter volley (move 4) is parked out of
        // every bag for now. The death squad (move 10) is Absolute Death's exclusive strong move.
        protected override int StrongMoveMask => (1 << 3) | (1 << 7) | (1 << 9) | (1 << 10);
        // Absolute Death keeps orbiting the player for the whole Strong5 move instead of planting
        // itself after the opening volley like normal Death does.
        protected override bool PhaseTwoStrong5SnapsThenHolds => false;
        internal override Color VfxGlowColor => new Color(190, 25, 50);

        public override void SetDefaults()
        {
            base.SetDefaults();

            NPC.damage = 2222;
            NPC.defense = 666;
            NPC.scale = 1.3f;
            NPC.lifeMax = 666666;
        }

        protected override void ApplyWorldOverrides()
        {
            Main.dayTime = false;
            Main.time = 3000;
            Main.bloodMoon = true;
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.BossBag(ModContent.ItemType<DeathBag>()));
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.NonExpertFirstKillRule, ModContent.ItemType<GuardianSoul>()));
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.NonExpertFirstKillRule, ModContent.ItemType<SoulVessel>()));
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.NonExpertFirstKillRule, ModContent.ItemType<StaminaVessel>()));
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.NonExpertFirstKillRule, ModContent.ItemType<HeroSoul>(), 2));

            IItemDropRule notExpertCondition = new LeadingConditionRule(new Conditions.NotExpert());
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<HolyWarElixir>(), 1, 3, 5));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<GreatMagicShieldScroll>(), 6));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<MagicBarrierScroll>()));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<Laevateinn>()));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<Humanity>()));
            npcLoot.Add(notExpertCondition);
        }
    }
}
