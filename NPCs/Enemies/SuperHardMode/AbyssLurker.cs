using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Content.Items.Materials;
using tsorcRevamp.Content.Items.Materials.Titanite;
using tsorcRevamp.Content.Items.Potions;
using tsorcRevamp.Content.Items.Potions.Lifegem;
using tsorcRevamp.Content.Items.Potions.StarlightShard;
using tsorcRevamp.Content.Projectiles.Enemy;

namespace tsorcRevamp.NPCs.Enemies.SuperHardMode
{
    // The SHM Lurker: the Fire Lurker's body in Pink Dye, purple flame, Abyss debuffs on every hit and no Fire Burst.
    // Its full crown never launches on its own; only a hit launches it.
    class AbyssLurker : LurkerPuppet
    {
        public int lostSoulDamage = 40;

        // Every Abyss Lurker hit drags the player into the Abyss (the Abyss debuff sets EnterTheAbyss) for this long.
        // Shared by the melee moves, AbyssLurkerFlameOrb, AbyssLurkerMeteor and the death Lost Soul (tsorcGlobalProjectile).
        public const int AbyssDebuffTicks = 30 * 60;
        // Abyssal Inferno on every hit, same set as above. 3s; it's a LongerExpertDebuff, so Expert/Master make it 6s.
        public const int AbyssInfernoTicks = 3 * 60;
        // Old contact-only extras, now applied by the melee moves. Vanilla doubles both in Expert/Master (LongerExpertDebuff).
        private const int MeleeBrokenArmorTicks = 3 * 60;
        private const int MeleeDarknessTicks = 3 * 60;

        // Old contact damage 70 raw -> 140 Expert, now dealt by Ember Rake / Leap Swipe.
        private const int MeleeExpert = 140;

        protected override int OrbType => ModContent.ProjectileType<AbyssLurkerFlameOrb>();
        protected override int MeteorType => ModContent.ProjectileType<AbyssLurkerMeteor>();
        protected override int LostSoulDamage => lostSoulDamage;
        protected override int HurlPeriod => 4 * 60;
        protected override float HurlSpeed => 2.4f;
        protected override int HurlTellTicks => 22;
        protected override bool CrownAutoLaunch => false;
        protected override bool HasFireBurst => false;
        protected override float BaseTopSpeed => 1.9f;
        protected override float BaseAcceleration => 0.08f;
        protected override float EnrageFraction => 0.4f;
        protected override float EnrageTopSpeed => 4f;
        protected override int MeleeExpertDamage => MeleeExpert;
        protected override int EmberDust => DustID.PinkTorch;
        protected override Color EmberColor => new Color(200, 90, 255);
        protected override int GroanFrequency => 900;

        // FighterAI scaled every SuperHardMode-namespace enemy's speed by SHMScale (and acceleration by the subtle
        // scale). SF4 puppets don't do that namespace check, so it is applied here explicitly.
        protected override float SpeedScale => tsorcRevampWorld.SHMScale;
        protected override float AccelScale => tsorcRevampWorld.SubtleSHMScale;
        protected override float PuppetDrawScale => 1.1f;
        protected override int HeadArmorDyeItemType => ItemID.PinkDye;
        protected override int BodyArmorDyeItemType => ItemID.PinkDye;
        protected override int LegsArmorDyeItemType => ItemID.PinkDye;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 15;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][ModContent.BuffType<AbyssInferno>()] = true;
        }

        public override void SetDefaults()
        {
            NPC.npcSlots = 5;
            NPC.knockBackResist = 0f;
            NPC.damage = 70;
            NPC.defense = 90;
            NPC.height = 40;
            NPC.width = 20;
            NPC.lifeMax = 1600;
            NPC.scale = 1.1f;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath5;
            NPC.value = 6000; // was 43 for 100 life
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.FireLurkerBanner>();
            ApplyLurkerDefaults();
            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.KiteRangeMin = 10f;
            globalNPC.KiteRangeMax = 25f;
            globalNPC.KiteLooseness = 0.5f;
        }

        public override void ApplyMeleeDebuffs(Player target)
        {
            target.AddBuff(ModContent.BuffType<AbyssInferno>(), AbyssInfernoTicks, false);
            target.AddBuff(ModContent.BuffType<Abyss>(), AbyssDebuffTicks, false);
            target.AddBuff(BuffID.BrokenArmor, MeleeBrokenArmorTicks, false);
            target.AddBuff(BuffID.Darkness, MeleeDarknessTicks, false);
        }

        public override void OnKill()
        {
            if (!Main.dedServ)
            {
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("AbyssLurkerGore1").Type, 1.1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("AbyssLurkerGore2").Type, 1.1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("AbyssLurkerGore3").Type, 1.1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("AbyssLurkerGore2").Type, 1.1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("AbyssLurkerGore3").Type, 1.1f);
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
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.CursedRule, ModContent.ItemType<LifegemItem>(), 4));
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.CursedRule, ModContent.ItemType<StarlightShardItem>(), 16));
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.SuperHardmodeRule, ModContent.ItemType<FlameOfTheAbyss>()));
            IItemDropRule drop = ItemDropRule.Common(ModContent.ItemType<RedTitanite>(), 1, 1, 2);
            SuperHardmodeRule SHM = new();
            IItemDropRule condition = new LeadingConditionRule(SHM);
            condition.OnSuccess(drop);
            npcLoot.Add(condition);
        }
    }
}
