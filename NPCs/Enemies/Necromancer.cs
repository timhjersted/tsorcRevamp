using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Potions;

namespace tsorcRevamp.NPCs.Enemies
{
    class Necromancer : ModNPC
    {
        int deathStrikeDamage = 18;
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 15;
        }
        public override void SetDefaults()
        {
            AnimationType = 21;
            NPC.knockBackResist = 0.2f;
            NPC.aiStyle = 3;
            NPC.damage = 0;
            NPC.defense = 25;
            NPC.height = 40;
            NPC.width = 20;
            NPC.lifeMax = 800;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.value = 4000; // was 270
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.NecromancerBanner>();
            UsefulFunctions.AddAttack(NPC, 120, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellSuddenDeathStrike>(), deathStrikeDamage, 8, SoundID.Item17);
            UsefulFunctions.AddAttack(NPC, 120, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellEffectHealing>(), 0, 0, SoundID.Item17);
        }

        float skeletonTimer;
        float skeletonsSpawned;
        //Spawns in the Underground and Cavern before 3.5/10ths and after 7.5/10ths (Width). Does not Spawn in the Jungle, Meteor, or if there are Town NPCs.

        #region Spawn
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            bool oSky = (spawnInfo.SpawnTileY < (Main.maxTilesY * 0.1f));
            bool oSurface = (spawnInfo.SpawnTileY >= (Main.maxTilesY * 0.1f) && spawnInfo.SpawnTileY < (Main.maxTilesY * 0.2f));
            bool oUnderSurface = (spawnInfo.SpawnTileY >= (Main.maxTilesY * 0.2f) && spawnInfo.SpawnTileY < (Main.maxTilesY * 0.3f));
            bool oUnderground = (spawnInfo.SpawnTileY >= (Main.maxTilesY * 0.3f) && spawnInfo.SpawnTileY < (Main.maxTilesY * 0.4f));
            bool oCavern = (spawnInfo.SpawnTileY >= (Main.maxTilesY * 0.4f) && spawnInfo.SpawnTileY < (Main.maxTilesY * 0.6f));
            bool oMagmaCavern = (spawnInfo.SpawnTileY >= (Main.maxTilesY * 0.6f) && spawnInfo.SpawnTileY < (Main.maxTilesY * 0.8f));
            bool oUnderworld = (spawnInfo.SpawnTileY >= (Main.maxTilesY * 0.8f));

            if (spawnInfo.Player.townNPCs > 0f || spawnInfo.Player.ZoneJungle || spawnInfo.Player.ZoneMeteor) return 0;

            if (spawnInfo.Water) return 0f;

            if (!Main.hardMode)
            {
                if (spawnInfo.Player.ZoneDungeon && Main.rand.NextBool(3000)) return 1;
                if (oUnderworld && Main.rand.NextBool(500)) return 1;
                if (oUnderworld && !Main.dayTime && Main.rand.NextBool(200)) return 1;
                if ((spawnInfo.SpawnTileX < Main.maxTilesX * 0.35f || spawnInfo.SpawnTileX > Main.maxTilesX * 0.75f) && (oUnderground || oCavern) && Main.rand.NextBool(2000)) return 1;
                return 0;
            }
            else if (Main.hardMode)
            {
                if (oUnderworld && Main.dayTime && Main.rand.NextBool(60)) return 1;
                if (oUnderworld && !Main.dayTime && Main.rand.NextBool(35)) return 1;
                if (spawnInfo.Player.ZoneDungeon && Main.rand.NextBool(100)) return 1;
                if (spawnInfo.Player.ZoneHallow && (oUnderground || oCavern) && Main.rand.NextBool(90)) return 1;
                if ((spawnInfo.Player.ZoneCorrupt || spawnInfo.Player.ZoneCrimson || spawnInfo.Player.ZoneUndergroundDesert) && (oUnderground || oCavern) && Main.rand.NextBool(100)) return 1;
                return 0;
            }
            return 0;
        }
        #endregion

        public override void AI()
        {
            tsorcRevampAIs.FighterAI(NPC, 1.5f, 0.05f, canTeleport: true, lavaJumping: true);

            skeletonTimer++;
            bool lineOfSight = Collision.CanHit(NPC.position, NPC.width, NPC.height, Main.player[NPC.target].position, Main.player[NPC.target].width, Main.player[NPC.target].height);

            if (NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().AttackSucceeded == 1)
            {
                NPC.life += 10;
                NPC.HealEffect(10);
                if (NPC.life > NPC.lifeMax)
                {
                    NPC.life = NPC.lifeMax;
                }
                NPC.netUpdate = true;
            }

            if (NPC.justHit)
            {
                NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().ProjectileTimer = 0;
            }

            if ((skeletonsSpawned < 11) && skeletonTimer > 600 && lineOfSight)
            {
                skeletonTimer = 0;
                skeletonsSpawned += 1;

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int spawnedNPC = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X, (int)NPC.Center.Y, NPCID.ArmoredSkeleton, 0);
                    if (Main.netMode == NetmodeID.Server)
                    {
                        NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, spawnedNPC, 0f, 0f, 0f, 0);
                    }
                }
            }
        }

        #region Gore
        public override void OnKill()
        {
            if (!Main.dedServ)
            {
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Necromancer Gore 1").Type, 1.1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Necromancer Gore 2").Type, 1.1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Necromancer Gore 3").Type, 1.1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Necromancer Gore 2").Type, 1.1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Necromancer Gore 3").Type, 1.1f);
            }
        }
        #endregion

        public override void ModifyNPCLoot(NPCLoot npcLoot) 
        {
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Items.Accessories.Mobility.BootsOfHaste>(), 30));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<CrimsonPotion>(), 40));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<StrengthPotion>(), 20));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<ShockwavePotion>(), 20));
            npcLoot.Add(ItemDropRule.Common(ItemID.IronskinPotion, 20));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<BattlefrontPotion>(), 25));
            npcLoot.Add(ItemDropRule.Common(ItemID.BloodMoonStarter, 25));
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.CursedRule, ModContent.ItemType<StarlightShard>(), 11));
        }
    }
}