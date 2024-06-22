using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Enemies
{
    public class Assassin : ModNPC
    {
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 20;
        }
        public override void SetDefaults()
        {
            AIType = NPCID.SkeletonArcher;
            NPC.HitSound = SoundID.NPCHit48;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.damage = 35; //normal mode stat
            NPC.lifeMax = 500; //normal mode stat
            if (tsorcRevampWorld.SuperHardMode)
            {
                NPC.lifeMax = 2000;
                NPC.defense = 60;
                NPC.damage = 80;
                NPC.value = 20000; // life / 1 bc rare : was 690
            }
            NPC.scale = 1.0f;
            NPC.defense = 40;
            NPC.value = 5000; // life / 1 bc rare : was 460
            NPC.width = 18;
            NPC.aiStyle = -1;
            NPC.height = 48;
            NPC.knockBackResist = 0.2f;
            NPC.rarity = 3;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.AssassinBanner>();

            AnimationType = NPCID.SkeletonArcher;
        }
        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Items.Ammo.ArrowOfBard>(), 6, 3, 5));
            npcLoot.Add(ItemDropRule.Common(ItemID.IronskinPotion, 25));
            npcLoot.Add(ItemDropRule.Common(ItemID.GreaterHealingPotion, 25));
            npcLoot.Add(ItemDropRule.Common(ItemID.ArcheryPotion, 25));
            npcLoot.Add(ItemDropRule.Common(ItemID.FlaskofCursedFlames, 25));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Items.Potions.CrimsonPotion>(), 30));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Items.Potions.StrengthPotion>(), 36));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Items.Potions.ShockwavePotion>(), 28));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Items.Potions.BattlefrontPotion>(), 25));
            npcLoot.Add(ItemDropRule.Common(ItemID.BloodMoonStarter, 50));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Items.Humanity>(), 22));
            npcLoot.Add(ItemDropRule.Common(ItemID.HolyArrow, 1, 100, 150));
            npcLoot.Add(ItemDropRule.Common(ItemID.UnicornHorn, 1, 1, 2));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Items.SoulCoin>(), 1, 3, 6));
            IItemDropRule hmCondition = new LeadingConditionRule(new Conditions.IsHardmode());
            hmCondition.OnSuccess(ItemDropRule.Common(ItemID.SoulofNight));
            npcLoot.Add(hmCondition);
        }


        //Never despawn except by timing out
        public override bool CheckActive()
        {
            return false;
        }

        #region Spawn
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            float chance = 0f;


            if (Main.hardMode && !Main.dayTime && spawnInfo.Player.ZoneJungle && !spawnInfo.Player.ZoneOverworldHeight && !spawnInfo.Player.ZoneDungeon && !spawnInfo.Player.ZoneCorrupt && !spawnInfo.Player.ZoneCrimson && Main.rand.NextBool(140))
            {
                if (Main.rand.NextBool(3))
                {
                    UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.Assassin.Track"), 175, 75, 255);
                }

                return 1f;
            }

            if (Main.hardMode && !Main.dayTime && spawnInfo.Player.ZoneJungle && !spawnInfo.Player.ZoneDungeon && !spawnInfo.Player.ZoneCorrupt && !spawnInfo.Player.ZoneCrimson && Main.rand.NextBool(200))
            {
                if (Main.rand.NextBool(3))
                {
                    UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.Assassin.Draw"), 175, 75, 255);
                }
                return 1f;
            }

            if (Main.hardMode && Main.dayTime && spawnInfo.Player.ZoneJungle && !spawnInfo.Player.ZoneDungeon && !spawnInfo.Player.ZoneCorrupt && !spawnInfo.Player.ZoneCrimson && Main.rand.NextBool(300))
            {
                if (Main.rand.NextBool(3))
                {
                    UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.Assassin.Steps"), 175, 75, 255);
                }
                return 1f;
            }

            if (Main.hardMode && (spawnInfo.Player.ZoneDungeon || spawnInfo.Player.ZoneHallow || spawnInfo.Player.ZoneSnow || spawnInfo.Player.ZoneUndergroundDesert || spawnInfo.Player.ZoneDesert) && Main.rand.NextBool(200))
            {
                if (Main.rand.NextBool(3))
                {
                    UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.Assassin.Track"), 175, 75, 255);
                }
                return 1f;
            }



            if (Main.hardMode && !Main.dayTime && spawnInfo.Player.ZoneOverworldHeight && Main.rand.NextBool(300))
            {
                if (Main.rand.NextBool(3))
                {
                    UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.Assassin.Hunt"), 175, 75, 255);
                }
                return 1f;
            }


            if (tsorcRevampWorld.SuperHardMode)
            {
                if (spawnInfo.Player.ZoneDirtLayerHeight)
                {
                    chance = 0.002f;
                }
                if (spawnInfo.Player.ZoneRockLayerHeight)
                {
                    chance = 0.001f;
                }
            }

            if (Main.bloodMoon)
            {
                chance *= 2;
            }

            return chance;
            //SUPER-HM

            /*if (ModWorld.superHardmode && !Main.dayTime && !Corruption && !Ocean && AboveEarth && Main.rand.NextBool(30))

			{
				UsefulFunctions.BroadcastText("An assassin is nearby...", 175, 75, 255);
				return true;
			}

			if (ModWorld.superHardmode && Main.dayTime && !Corruption && !Ocean && Jungle && AboveEarth && Main.rand.NextBool(30))

			{
				UsefulFunctions.BroadcastText("An assassin is nearby...", 175, 75, 255);
				return true;
			}

			if (ModWorld.superHardmode && !Corruption && !Ocean && Jungle && InGrayLayer && Main.rand.NextBool(20))

			{
				UsefulFunctions.BroadcastText("An assassin is tracking your position...", 175, 75, 255);
				return true;
			}            
             
            return chance;*/

        }

        #endregion

        public override void OnHitByItem(Player player, Item item, NPC.HitInfo hit, int damageDone)
        {

            tsorcRevampAIs.FighterOnHit(NPC, true);

            if (Main.rand.NextBool(15))
            {
                tsorcRevampAIs.TeleportImmediately(NPC, 30, false);
            }

        }

        public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            tsorcRevampAIs.FighterOnHit(NPC, projectile.DamageType == DamageClass.Melee);

            if (Main.rand.NextBool(15))
            {
                tsorcRevampAIs.TeleportImmediately(NPC, 40, false);
            }

        }

        public override void AI()
        {
            tsorcRevampAIs.ArcherAI(NPC, ModContent.ProjectileType<Projectiles.Enemy.EnemyArrowOfBard>(), 50, 14, 100, 2f, .05f, canTeleport: true, enragePercent: 0.4f, enrageTopSpeed: 4f, telegraphColor: Color.Green);
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
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Assassin Gore 1").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Assassin Gore 2").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Assassin Gore 3").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Assassin Gore 2").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Assassin Gore 3").Type, 1f);
                }
            }
        }
        #endregion
    }
}