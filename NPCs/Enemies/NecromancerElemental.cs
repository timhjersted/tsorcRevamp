using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Enemies
{
    class NecromancerElemental : ModNPC
    {
        int deathStrikeDamage = 18;
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 15;
        }
        public override void SetDefaults()
        {
            AnimationType = 21;
            NPC.knockBackResist = 0f;
            NPC.aiStyle = 3;
            NPC.damage = 35;
            NPC.defense = 30;
            NPC.height = 40;
            NPC.width = 20;
            NPC.lifeMax = 4000;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.lavaImmune = true;
            NPC.value = 20000;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.NecromancerElementalBanner>();
            UsefulFunctions.AddAttack(NPC, 120, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellSuddenDeathStrike>(), deathStrikeDamage, 8, SoundID.Item17);
            UsefulFunctions.AddAttack(NPC, 120, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellEffectHealing>(), 0, 0, SoundID.Item17);
        }
        //Spawns in the Underground and Cavern before 4.5/10ths and after 7.5/10ths (Width). Does not Spawn in the Jungle, Meteor, or if there are Town NPCs.

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


            if (Main.hardMode && (oUnderground || oCavern))
            {
                if ((spawnInfo.SpawnTileX < Main.maxTilesX * 0.45f || spawnInfo.SpawnTileX > Main.maxTilesX * 0.75f) && Main.rand.NextBool(350)) return 1;
            }

            else if (Main.hardMode && oUnderworld)
            {
                if (Main.rand.NextBool(150)) return 1;
            }

            return 0;
        }
        #endregion

        int chaosElementalTimer;
        int chaosElementalsSpawned = 0;
        public override void AI()
        {
            tsorcRevampAIs.FighterAI(NPC, 1.8f, 0.05f, canTeleport: true, lavaJumping: true);

            chaosElementalTimer++;

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

            bool lineOfSight = Collision.CanHit(NPC.position, NPC.width, NPC.height, Main.player[NPC.target].position, Main.player[NPC.target].width, Main.player[NPC.target].height) && Vector2.Distance(NPC.Center, Main.player[NPC.target].Center) <= 1000;
            if ((chaosElementalsSpawned < 11) && chaosElementalTimer > 300 && lineOfSight)
            {
                chaosElementalTimer = 0;
                chaosElementalsSpawned += 1;

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int spawnedNPC = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X, (int)NPC.Center.Y, NPCID.ChaosElemental, 0);
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
            npcLoot.Add(ItemDropRule.Common(ItemID.ManaRegenerationBand, 10));
            npcLoot.Add(ItemDropRule.Common(ItemID.GreaterHealingPotion, 10));
            npcLoot.Add(ItemDropRule.Common(ItemID.IronskinPotion, 20));
            npcLoot.Add(ItemDropRule.Common(ItemID.MagicPowerPotion, 25));
            npcLoot.Add(ItemDropRule.Common(ItemID.RegenerationPotion, 25));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Items.Potions.CrimsonPotion>(), 50));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Items.Potions.StrengthPotion>(), 50));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Items.Potions.ShockwavePotion>(), 50));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Items.Potions.BattlefrontPotion>(), 50));

        }
    }
}