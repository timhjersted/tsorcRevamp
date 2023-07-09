using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Potions;

namespace tsorcRevamp.NPCs.Enemies
{
    class ClericOfSorrow : ModNPC
    {
        //int meteorDamage = 9;
        public int iceBallDamage = 20;
        public int iceStormDamage = 18;
        public int lightningDamage = 18;
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 16;
        }
        public override void SetDefaults()
        {
            AnimationType = 28;
            NPC.knockBackResist = 0.01f;
            NPC.aiStyle = 3;
            NPC.damage = 65;
            NPC.defense = 35;
            NPC.height = 40;
            NPC.width = 20;
            NPC.lifeMax = 405;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.value = 1800;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.ClericOfSorrowBanner>();
            UsefulFunctions.AddAttack(NPC, 120, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellLightning3Ball>(), lightningDamage, 9, SoundID.Item17, 0.1f, 120, 1, telegraphColor: Color.Blue);
            UsefulFunctions.AddAttack(NPC, 240, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellIcestormBall>(), iceStormDamage, 8, SoundID.Item17);
            UsefulFunctions.AddAttack(NPC, 160, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellIce3Ball>(), iceBallDamage, 12, SoundID.Item17, 0, 1, 0, new Vector2(0, -240), Color.Cyan);
        }

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

            if (Main.hardMode && !Meteor && !Jungle && !Dungeon && !Corruption && Hallow && Main.rand.NextBool(55)) return 1;

            if (Main.hardMode && !Meteor && !Jungle && !Dungeon && !Corruption && Hallow && InBrownLayer && Main.rand.NextBool(35)) return 1;

            if (Main.hardMode && !Meteor && !Jungle && !Dungeon && !Corruption && Hallow && InGrayLayer && Main.rand.NextBool(25)) return 1;

            if (Main.hardMode && FrozenOcean && Main.rand.NextBool(20)) return 1;


            return 0;
        }
        #endregion


        public override void AI()
        {
            tsorcRevampAIs.FighterAI(NPC, 1.2f, 0.07f, 0.2f, true, enragePercent: 0.2f, enrageTopSpeed: 3);
            tsorcRevampAIs.LeapAtPlayer(NPC, 4, 3, 1, 100);
            NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().AttackList[2].ai1 = NPC.target; //Set the lightning ball it shoots to have an ai1 of the NPCs target, so it explodes when near it
        }

        #region Gore
        public override void OnKill()
        {
            if (!Main.dedServ)
            {
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Dark Elf Magi Gore 1").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Dark Elf Magi Gore 2").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Dark Elf Magi Gore 3").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Dark Elf Magi Gore 2").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Dark Elf Magi Gore 3").Type, 1f);
            }
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot) 
        {
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Items.Weapons.Melee.Rods.ForgottenIceRod>(), 20));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Items.Weapons.Melee.Rods.ForgottenThunderRod>(), 20));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Items.Weapons.Melee.Rods.ForgottenStardustRod>(), 100));
            npcLoot.Add(ItemDropRule.Common(ItemID.IronskinPotion, 30));
            npcLoot.Add(ItemDropRule.Common(ItemID.ManaRegenerationPotion, 35));
            npcLoot.Add(ItemDropRule.Common(ItemID.GreaterHealingPotion, 20));
            npcLoot.Add(new CommonDrop(ItemID.GillsPotion, 100, 1, 1, 6));
            npcLoot.Add(new CommonDrop(ItemID.HunterPotion, 100, 1, 1, 6));
            npcLoot.Add(ItemDropRule.Common(ItemID.MagicPowerPotion, 25));
            npcLoot.Add(ItemDropRule.Common(ItemID.ShinePotion, 25));
            npcLoot.Add(ItemDropRule.Common(ItemID.SoulofNight, 2));
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.CursedRule, ModContent.ItemType<StarlightShard>(), 7));
        }
        #endregion

    }
}