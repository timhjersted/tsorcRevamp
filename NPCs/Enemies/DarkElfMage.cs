using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Enemies
{
    class DarkElfMage : ModNPC
    {
        public override void SetDefaults()
        {
            Main.npcFrameCount[NPC.type] = 16;
            AnimationType = 28;
            NPC.knockBackResist = 0.01f;
            NPC.aiStyle = 3;
            NPC.damage = 76;
            NPC.defense = 35;
            NPC.height = 40;
            NPC.width = 20;
            NPC.lifeMax = 810;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.value = 1800;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.DarkElfMageBanner>();
        }

        public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
        {
            NPC.lifeMax = (int)(NPC.lifeMax / 2);
            NPC.damage = (int)(NPC.damage / 2);
            meteorDamage = (int)(meteorDamage / 2);
            iceBallDamage = (int)(iceBallDamage / 2);
            iceStormDamage = (int)(iceStormDamage / 2);
            lightningDamage = (int)(lightningDamage / 2);
        }

        int meteorDamage = 17;
        int iceBallDamage = 40;
        int iceStormDamage = 35;
        int lightningDamage = 35;




        //Spawns in Hardmode Surface and Underground, 6.5/10th of the world to the right edge (Width). Does not spawn in Dungeons, Jungle, or Meteor. Only spawns with Town NPCs during Blood Moons.

        #region Spawn
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            Player P = spawnInfo.Player; //this shortens our code up from writing this line over and over.

            bool Sky = spawnInfo.SpawnTileY <= (Main.rockLayer * 4);
            bool Meteor = P.ZoneMeteor;
            bool Jungle = P.ZoneJungle;
            bool Dungeon = P.ZoneDungeon;
            bool Corruption = (P.ZoneCorrupt || P.ZoneCrimson);
            bool Hallow = P.ZoneHallow;
            bool AboveEarth = P.ZoneOverworldHeight;
            bool InBrownLayer = P.ZoneDirtLayerHeight;
            bool InGrayLayer = P.ZoneRockLayerHeight;
            bool InHell = P.ZoneUnderworldHeight;
            bool FrozenOcean = spawnInfo.SpawnTileX > (Main.maxTilesX - 800);
            bool Ocean = spawnInfo.SpawnTileX < 800 || FrozenOcean;

            // these are all the regular stuff you get , now lets see......
            if (spawnInfo.Player.townNPCs > 0f) return 0;

            if (Main.hardMode && !Meteor && !Jungle && !Dungeon && !Corruption && Hallow && Main.rand.Next(55) == 1) return 1;

            if (Main.hardMode && !Meteor && !Jungle && !Dungeon && !Corruption && Hallow && InBrownLayer && Main.rand.Next(35) == 1) return 1;

            if (Main.hardMode && !Meteor && !Jungle && !Dungeon && !Corruption && Hallow && InGrayLayer && Main.rand.Next(25) == 1) return 1;

            if (Main.hardMode && FrozenOcean && Main.rand.Next(20) == 1) return 1;


            return 0;
        }
        #endregion


        public override void AI()
        {
            tsorcRevampAIs.FighterAI(NPC, 2, 0.07f, 0.2f, true, enragePercent: 0.2f, enrageTopSpeed: 3);
            tsorcRevampAIs.LeapAtPlayer(NPC, 4, 3, 1, 100);

            NPC.localAI[1]++;
            bool validTarget = Collision.CanHit(NPC.position, NPC.width, NPC.height, Main.player[NPC.target].position, Main.player[NPC.target].width, Main.player[NPC.target].height);
            tsorcRevampAIs.SimpleProjectile(NPC, ref NPC.localAI[1], 90, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellLightning3Ball>(), lightningDamage, 9, validTarget, false, SoundID.Item17, 0.1f, 120, 1);
            tsorcRevampAIs.SimpleProjectile(NPC, ref NPC.localAI[1], 90, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellIcestormBall>(), iceStormDamage, 8, validTarget, false, SoundID.Item17);


            if (NPC.localAI[1] >= 90 && validTarget)
            {
                NPC.localAI[1] = 0;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 overshoot = new Vector2(0, -240);
                    Vector2 projectileVector = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].Center + overshoot, 12, 0.035f);
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, projectileVector.X, projectileVector.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellIce3Ball>(), iceBallDamage, 0f, Main.myPlayer, 1, NPC.target);
                }

                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);

            }
        }

        #region Gore
        public override void OnKill()
        {

            Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Dark Elf Magi Gore 1").Type, 1f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Dark Elf Magi Gore 2").Type, 1f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Dark Elf Magi Gore 3").Type, 1f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Dark Elf Magi Gore 2").Type, 1f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Dark Elf Magi Gore 3").Type, 1f);

            if (Main.rand.Next(100) < 5) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Weapons.Melee.ForgottenIceRod>());
            if (Main.rand.Next(100) < 5) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Weapons.Melee.ForgottenThunderRod>());
            if (Main.rand.Next(100) < 1) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Weapons.Melee.ForgottenStardustRod>());
            if (Main.rand.Next(100) < 10) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.IronskinPotion);
            if (Main.rand.Next(100) < 20) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.ManaRegenerationPotion);
            if (Main.rand.Next(100) < 10) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.GreaterHealingPotion);
            if (Main.rand.Next(100) < 6) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.GillsPotion);
            if (Main.rand.Next(100) < 6) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.HunterPotion);
            if (Main.rand.Next(100) < 20) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.MagicPowerPotion);
            if (Main.rand.Next(100) < 20) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.ShinePotion);

        }
        #endregion

    }
}