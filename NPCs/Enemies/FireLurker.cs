using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items;
using tsorcRevamp.Content.Items.ConsumableSoul;
using tsorcRevamp.Content.Items.Materials;
using tsorcRevamp.Content.Items.Materials.Titanite;
using tsorcRevamp.Content.Items.Potions;
using tsorcRevamp.Content.Items.Potions.Lifegem;
using tsorcRevamp.Content.Items.Potions.StarlightShard;
using tsorcRevamp.Content.Projectiles.Enemy;

namespace tsorcRevamp.NPCs.Enemies
{
    class FireLurker : LurkerPuppet
    {
        // Written by the Path of Pain scripted event (FireLurkerPainCustomAction), so it stays a public field.
        public int lostSoulDamage = 8;

        // Ember Rake / Leap Swipe hit for the old contact damage: 20 / 30 / 65 raw contact -> 40 / 60 / 130 Expert.
        private int meleeExpertDamage = 40;

        // Fire Burst keeps double the old contact: 20 / 30 / 65 raw projectile -> 80 / 120 / 260 Expert.
        private int fireBurstExpertDamage = 80;

        // Old contact debuffs, all guaranteed, now applied by the melee moves. Vanilla doubles each in Expert/Master.
        private const int MeleeDebuffTicks = 2 * 60;

        protected override int OrbType => ModContent.ProjectileType<FireLurkerFlameOrb>();
        protected override int MeteorType => ModContent.ProjectileType<FireLurkerMeteor>();
        protected override int LostSoulDamage => lostSoulDamage;
        protected override int HurlPeriod => 6 * 60;
        protected override float HurlSpeed => 2.1f;
        protected override int HurlTellTicks => 25;
        protected override bool CrownAutoLaunch => true;
        protected override bool HasFireBurst => true;
        protected override float BaseTopSpeed => 1.5f;
        protected override float BaseAcceleration => 0.07f;
        protected override float EnrageFraction => 0.36f;
        protected override float EnrageTopSpeed => 3f;
        protected override int MeleeExpertDamage => meleeExpertDamage;
        protected override int FireBurstExpertDamage => fireBurstExpertDamage;
        protected override int EmberDust => DustID.Torch;
        protected override Color EmberColor => new Color(255, 135, 35);
        protected override int GroanFrequency => 1000;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 15;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire] = true;
        }

        public override void SetDefaults()
        {
            NPC.npcSlots = 3;
            NPC.knockBackResist = 0.4f;
            NPC.damage = 20;
            NPC.defense = 10;
            NPC.height = 40;
            NPC.width = 20;
            NPC.lifeMax = 120;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath5;
            NPC.value = 600; // was 43 for 100 life
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.FireLurkerBanner>();
            ApplyLurkerDefaults();
            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.KiteRangeMin = 5f;
            globalNPC.KiteRangeMax = 25f;
            globalNPC.KiteLooseness = 0.9f;

            if (Main.hardMode)
            {
                NPC.lifeMax = 250;
                NPC.defense = 22;
                NPC.value = 1250; // was 65
                NPC.damage = 30;
                lostSoulDamage = 22;
                meleeExpertDamage = 60;
                fireBurstExpertDamage = 120;
                NPC.knockBackResist = 0.2f;
            }

            if (tsorcRevampWorld.SuperHardMode)
            {
                NPC.lifeMax = 1200;
                NPC.defense = 70;
                NPC.value = 5000; // was 365 with 1330 life
                NPC.damage = 65;
                lostSoulDamage = 35;
                meleeExpertDamage = 130;
                fireBurstExpertDamage = 260;
                NPC.knockBackResist = 0.05f;
            }
        }

        public override void ApplyMeleeDebuffs(Player target)
        {
            target.AddBuff(BuffID.OnFire, MeleeDebuffTicks, false);
            target.AddBuff(BuffID.BrokenArmor, MeleeDebuffTicks, false);
            target.AddBuff(BuffID.Darkness, MeleeDebuffTicks, false);
        }

        public override void OnKill()
        {
            if (!Main.dedServ)
            {
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("FireLurkerGore1").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("FireLurkerGore2").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("FireLurkerGore3").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("FireLurkerGore2").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("FireLurkerGore3").Type, 1f);
                for (int i = 0; i < 10; i++)
                {
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Blood Splat").Type, 1.1f);
                }
            }

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Player target = Main.player[Player.FindClosest(NPC.position, NPC.width, NPC.height)];
                if (target.active && !target.dead)
                {
                    Vector2 lostSoulVelocity = UsefulFunctions.Aim(NPC.Center, target.Center, 6f);
                    Projectile.NewProjectile(NPC.GetSource_Death(), NPC.Center, lostSoulVelocity, ProjectileID.LostSoulHostile, lostSoulDamage, 0, Main.myPlayer);
                }
            }
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.Common(ItemID.GreaterHealingPotion, 12));
            npcLoot.Add(new CommonDrop(ItemID.ManaRegenerationPotion, 100, 1, 1, 30));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<FadingSoul>(), 20));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<CharcoalPineResin>(), 10));
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.CursedRule, ModContent.ItemType<LifegemItem>(), 4));
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.CursedRule, ModContent.ItemType<StarlightShardItem>(), 16));
            IItemDropRule drop = ItemDropRule.Common(ModContent.ItemType<RedTitanite>(), 1, 1, 2);
            SuperHardmodeRule SHM = new();
            IItemDropRule condition = new LeadingConditionRule(SHM);
            condition.OnSuccess(drop);
            npcLoot.Add(condition);
        }
    }
}
