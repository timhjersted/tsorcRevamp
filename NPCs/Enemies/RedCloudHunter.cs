using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Enemies
{
    public class RedCloudHunter : ModNPC
    {
        public int archerBoltDamage = 30; //was 85, whoa, how did no one complain about this?
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 20;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
        }
        public override void SetDefaults()
        {
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.damage = 18;
            NPC.lifeMax = 600;
            NPC.defense = 18;
            NPC.value = 5210; // life / 1.15 bc rare : was 650
            NPC.width = 18;
            NPC.aiStyle = -1;
            NPC.height = 48;
            NPC.knockBackResist = 0.6f;
            NPC.scale = 1.05f;
            NPC.rarity = 3;
            Banner = NPC.type;
            NPC.buffImmune[BuffID.Confused] = true;
            BannerItem = ModContent.ItemType<Banners.RedCloudHunterBanner>();
            AnimationType = NPCID.SkeletonArcher;
            if (Main.hardMode)
            {
                NPC.lifeMax = 700;
                NPC.defense = 40;
                NPC.value = 4000; // was 350
                NPC.damage = 24;
                archerBoltDamage = 45;
            }
            if (tsorcRevampWorld.SuperHardMode)
            {
                NPC.lifeMax = 2000;
                NPC.defense = 58;
                NPC.value = 8000; // life / 2.5 : was 390
                NPC.damage = 55;
                archerBoltDamage = 75;
            }

            // Navigation tuning: above-average jumps and ledge routing for a mobile archer
            tsorcRevampGlobalNPC hunterGlobalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            hunterGlobalNPC.MaxJumpPower = 12f;
            hunterGlobalNPC.MaxJumpBoost = 5f;
            // CanDoubleJump remains false for RedCloudHunter
            // Step 6 archer levers: blink to elevated firing spots, and reposition toward last-known before patrolling.
            hunterGlobalNPC.PrefersHighGround = true;
            hunterGlobalNPC.RemembersLastKnownPos = true;
            hunterGlobalNPC.NavSearchRadius = 80;
            hunterGlobalNPC.KiteRangeMin = 10f;
            hunterGlobalNPC.KiteRangeMax = 40f;
            hunterGlobalNPC.KiteLooseness = 0.2f;
            hunterGlobalNPC.CanGoInvisible = true;
            hunterGlobalNPC.InvisibleAlpha = 200;
            EvasiveProfile.EvasiveCloak(hunterGlobalNPC, cloakChance: 0.20f, threatRange: 220);
            // Poise (a stagger guarantees a cloak reveal) + knockback flinch are tuned centrally in
            // tsorcRevampGlobalNPC.PopulatePoiseProfiles() (GlobalNPC.cs) — not here.
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Items.Humanity>(), 6));
            npcLoot.Add(ItemDropRule.Common(ItemID.AmmoReservationPotion, 25));
            npcLoot.Add(ItemDropRule.Common(ItemID.HolyArrow, 1, 30, 60));
            npcLoot.Add(ItemDropRule.Common(ItemID.UnicornHorn, 3, 1, 1));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Items.SoulCoin>(), 1, 6, 8));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Items.Weapons.Summon.ArcherSpiritBell>(), 1));
        }

        #region Spawn

        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            float chance = 0f;


            if (!Main.hardMode && spawnInfo.Player.ZoneDungeon) return 0.01f;

            if (Main.hardMode && !spawnInfo.Player.ZoneCorrupt && !spawnInfo.Player.ZoneCrimson && !spawnInfo.Player.ZoneBeach && spawnInfo.Player.ZoneJungle) return 0.02f;
            if (Main.hardMode && spawnInfo.Player.ZoneHallow && !spawnInfo.Player.ZoneDungeon) return 0.01f;
            if (Main.hardMode && spawnInfo.Player.ZoneOverworldHeight && (spawnInfo.Player.ZoneDesert || spawnInfo.Player.ZoneCorrupt || spawnInfo.Player.ZoneCrimson || spawnInfo.Player.ZoneBeach || spawnInfo.Player.ZoneJungle)) return 0.0125f;

            if (Main.hardMode && spawnInfo.Lihzahrd) return 0.15f;

            if (tsorcRevampWorld.SuperHardMode && (spawnInfo.Player.ZoneCorrupt || spawnInfo.Player.ZoneCrimson)) return 0.13f;
            if (tsorcRevampWorld.SuperHardMode && spawnInfo.Player.ZoneOverworldHeight && (spawnInfo.Player.ZoneJungle || spawnInfo.Player.ZoneCorrupt || spawnInfo.Player.ZoneCrimson)) return 0.1f;
            if (tsorcRevampWorld.SuperHardMode && (spawnInfo.Player.ZoneDesert || spawnInfo.Player.ZoneUndergroundDesert)) return 0.13f;
            if (tsorcRevampWorld.SuperHardMode && spawnInfo.Player.ZoneDungeon) return 0.01f; //.08% is 4.28%
            return chance;
        }
        #endregion

        public override void AI()
        {
            tsorcRevampAIs.ArcherAI(NPC, ModContent.ProjectileType<Projectiles.Enemy.EnemyFrostburnArrow>(), archerBoltDamage, 13, 100, 2, canTeleport: true, enragePercent: 0.3f, enrageTopSpeed: 2.6f, telegraphColor: Color.Red);
        }

        // SkeletonArcher's VanillaFindFrame gates walk frames on strict velocity.Y == 0f, so any
        // tiny residual Y from SmartFighter4AI causes the NPC to show frame 0 (idle) while moving.
        // This override replicates case 110 exactly but accepts collideY as an additional grounded indicator.
        public override void FindFrame(int frameHeight)
        {
            NPC.spriteDirection = NPC.direction;
            bool grounded = NPC.velocity.Y == 0f || NPC.collideY;

            if (grounded)
            {
                // Shooting animation: ai[2] is the frame index set by ArcherAI while aiming/firing.
                if (NPC.ai[2] > 0f)
                {
                    NPC.frame.Y = frameHeight * (int)NPC.ai[2];
                    NPC.frameCounter = 0.0;
                    return;
                }
                // Walk frames begin at frame 6; counter advances with horizontal speed.
                if (NPC.frame.Y < frameHeight * 6)
                    NPC.frame.Y = frameHeight * 6;
                NPC.frameCounter += Math.Abs(NPC.velocity.X) * 2.0 + NPC.velocity.X;
                if (NPC.frameCounter > 6.0)
                {
                    NPC.frame.Y += frameHeight;
                    NPC.frameCounter = 0.0;
                }
                if (NPC.frame.Y / frameHeight >= Main.npcFrameCount[NPC.type])
                    NPC.frame.Y = frameHeight * 6;
            }
            else
            {
                NPC.frameCounter = 0.0;
                NPC.frame.Y = 0;
            }
        }

        #region Gore
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
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Red Cloud Hunter Gore 1").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Red Cloud Hunter Gore 2").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Red Cloud Hunter Gore 3").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Red Cloud Hunter Gore 2").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Red Cloud Hunter Gore 3").Type, 1f);
                }
            }
        }
        #endregion
    }
}
