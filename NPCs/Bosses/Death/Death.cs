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
using tsorcRevamp.Content.Projectiles.Enemy;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Bosses.Death
{
    [AutoloadBossHead]
    class Death : DeathBossBase
    {
        protected override Color AuraColor => Color.MediumPurple;
        protected override int PhaseOneBoltCount => 0;
        protected override Color PhaseOneBoltColor => new Color(165, 40, 255);
        protected override Color AuraDustColor => new Color(58, 12, 82);
        protected override Color DespawnTextColor => Color.DarkMagenta;
        protected override int DespawnDustType => DustID.Demonite;
        protected override int VolleyProjectileType => ModContent.ProjectileType<ShadowShot>();
        // EnemyDamage.Projectile declares the Expert pre-defense hit; vanilla applies Normal/Master scaling.
        protected override int BoltDamage => EnemyDamage.Projectile(222);
        protected override int SickleDamage => EnemyDamage.Projectile(222);
        protected override int GiantScytheDamage => 222;
        protected override int BodyContactDamage => 222;
        // Reaper contact is declared as an Expert baseline for EnemyDamage.SetContact.
        protected override int GetReaperContactDamage() => 222;
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
