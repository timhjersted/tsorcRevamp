using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Potions;

namespace tsorcRevamp.NPCs.Enemies
{
    public class ManHunter : ModNPC
    {
        public int archerBoltDamage = 20;
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 20;
        }
        public override void SetDefaults()
        {
            AIType = NPCID.SkeletonArcher;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.damage = 12;
            NPC.lifeMax = 125;
            NPC.defense = 9;
            NPC.value = 630; // was 100
            NPC.scale = 0.9f;
            NPC.width = 18;
            NPC.aiStyle = -1;
            NPC.height = 48;
            NPC.knockBackResist = 0.6f;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.ManHunterBanner>();

            AnimationType = NPCID.SkeletonArcher;

            if (Main.hardMode)
            {
                NPC.lifeMax = 250;
                NPC.defense = 22;
                NPC.value = 1250; // was 150
                NPC.damage = 25;
                archerBoltDamage = 28;
            }

            if (tsorcRevampWorld.SuperHardMode)
            {
                NPC.lifeMax = 600;
                NPC.defense = 38;
                NPC.value = 1500;
                NPC.damage = 50;
                archerBoltDamage = 40;
            }

            // Step 6 archer levers. ManHunter has no teleport, so PrefersHighGround stays dormant until the
            // patrol high-ground bias lands; RemembersLastKnownPos makes it reposition toward where it last saw you.
            tsorcRevampGlobalNPC archerGlobalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            archerGlobalNPC.PrefersHighGround = true;
            archerGlobalNPC.RemembersLastKnownPos = true;
            archerGlobalNPC.NavSearchRadius = 50; // Phase 2: SmartFighter4AI movement
            archerGlobalNPC.CanUseRopes = true;
            archerGlobalNPC.KiteRangeMin = 8f;
            archerGlobalNPC.KiteRangeMax = 22f;
            archerGlobalNPC.KiteLooseness = 0.35f;
            archerGlobalNPC.CanAdvanceAndShoot = true;
            // Uses the fixed vanilla Skeleton Archer sheet: it has aim/fire frames but no walk-while-firing frames.
            // AdvanceAndShoot may accelerate its approach between shots, but stop-to-fire must remain enabled.
            archerGlobalNPC.CanFireWhileAdvancing = false;
            archerGlobalNPC.AdvanceAndShootSpeedMultiplier = 1.45f;
            archerGlobalNPC.AdvanceAndShootAccelerationMultiplier = 1.4f;

            // The forward branch of QuickStep is the shared flank move: cross completely through the target and
            // leave a three-tile gap, while retaining the ordinary backstep when a safe cross is out of reach.
            archerGlobalNPC.EvasiveQuickStep = true;
            archerGlobalNPC.QuickStepSpeed = 6f;
            archerGlobalNPC.QuickStepForwardRoom = 48f;
            archerGlobalNPC.QuickStepForwardChance = 0.8f;
            archerGlobalNPC.QuickStepMaxForwardTicks = 36;
            CombatTempoProfile.Standard(archerGlobalNPC, ProjectileID.FlamingArrow);
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.Common(ItemID.ArcheryPotion));
            npcLoot.Add(ItemDropRule.Common(ItemID.HunterPotion, 34));
            npcLoot.Add(ItemDropRule.Common(ItemID.AmmoReservationPotion, 34));
            npcLoot.Add(ItemDropRule.Common(ItemID.SwiftnessPotion, 34));
            npcLoot.Add(new CommonDrop(ItemID.HealingPotion, 100, 1, 1, 2));
            npcLoot.Add(ItemDropRule.Common(ItemID.UnholyArrow, 4, 10, 20));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Items.SoulCoin>(), 2, 1, 2));
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.CursedRule, ModContent.ItemType<Lifegem>(), 8));
        }


        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            float chance = 0f;

            if (!Main.hardMode && !spawnInfo.Player.ZoneMeteor && spawnInfo.Player.ZoneJungle && !spawnInfo.Player.ZoneDungeon && !spawnInfo.Player.ZoneCorrupt && !spawnInfo.Player.ZoneCrimson)
            {
                if (spawnInfo.Player.ZoneOverworldHeight) return 0.1f;
                if (spawnInfo.Player.ZoneDirtLayerHeight) return 0.03f;
                if (spawnInfo.Player.ZoneRockLayerHeight) return 0.04f;
            }
            if (Main.hardMode && !spawnInfo.Player.ZoneMeteor && !spawnInfo.Player.ZoneBeach && !spawnInfo.Player.ZoneCorrupt && !spawnInfo.Player.ZoneCrimson) return 0.02f;

            return chance;
        }

        public override void AI()
        {
            tsorcRevampAIs.ArcherAI(NPC, ProjectileID.FlamingArrow, 14, 11, 120, 1.3f, 0.08f, canTeleport: false);
        }



        public override void OnHitByItem(Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            tsorcRevampAIs.EvasiveOnHit(NPC, true);
        }

        public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            tsorcRevampAIs.EvasiveOnHit(NPC, projectile.DamageType == DamageClass.Melee);
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            for (int i = 0; i < 5; i++)
            {
                int DustType = 5;
                int dustIndex = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustType);
                Dust dust = Main.dust[dustIndex];
                dust.velocity.X += Main.rand.Next(-50, 51) * 0.06f;
                dust.velocity.Y += Main.rand.Next(-50, 51) * 0.06f;
                dust.scale *= 1f + Main.rand.Next(-30, 31) * 0.01f;
                dust.noGravity = true;
            }
            if (NPC.life <= 0)
            {
                for (int i = 0; i < 25; i++)
                {
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, 5, Main.rand.Next(-3, 3), Main.rand.Next(-3, 3), 70, default(Color), 1f);
                }
                if (!Main.dedServ)
                {
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Man Hunter Gore 1").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Man Hunter Gore 2").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Man Hunter Gore 3").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Man Hunter Gore 2").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Man Hunter Gore 3").Type, 1f);
                }
            }
        }
    }
}