using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;

namespace tsorcRevamp.NPCs.Enemies
{
	class AncientDemon : ModNPC
	{
		public override void SetDefaults()
		{
			Main.npcFrameCount[NPC.type] = 16;
			animationType = 28;
			NPC.height = 120;
			NPC.width = 50;
			NPC.damage = 55;
			NPC.defense = 15;
			NPC.lifeMax = 9000;
			NPC.scale = 1;
			NPC.HitSound = SoundID.NPCHit1;
			NPC.DeathSound = SoundID.NPCDeath5;
			NPC.value = 51000;
			NPC.knockBackResist = 0;
			NPC.lavaImmune = true;

			NPC.buffImmune[BuffID.Confused] = true;
			NPC.buffImmune[BuffID.OnFire] = true;
			NPC.buffImmune[BuffID.Poisoned] = true;
			NPC.buffImmune[BuffID.CursedInferno] = true;
		}

		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Ancient Demon");
		}

		int meteorDamage = 17;
		int poisonFireDamage = 25;
		int energyBeamDamage = 50;
		int fireBreathDamage = 50;
		int greatFireballDamage = 35;
		int blackFireDamage = 45;
		int greatAttackDamage = 75;

		public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
		{
			NPC.damage = (int)(NPC.damage / 2);
			meteorDamage = (int)(meteorDamage / 2);
			poisonFireDamage = (int)(poisonFireDamage / 2);
			energyBeamDamage = (int)(energyBeamDamage / 2);
			fireBreathDamage = (int)(fireBreathDamage / 2);
			greatFireballDamage = (int)(greatFireballDamage / 2);
			blackFireDamage = (int)(blackFireDamage / 2);
			greatAttackDamage = (int)(greatAttackDamage / 2);
		}

		//Spawns in Lower Cavern into the Underworld. Spawns more under 2.5/10th and again after 7.5/10th (Length). Spawns more in Hardmode. Will not spawn if there are more than 2 Town NPCs nearby (or if a Blood Moon).

		#region Spawn
		public override float SpawnChance(NPCSpawnInfo spawnInfo)
		{ 
			Player p = spawnInfo.Player;
			
			bool oSky = (spawnInfo.SpawnTileY < (Main.maxTilesY * 0.1f));
			bool oSurface = (spawnInfo.SpawnTileY >= (Main.maxTilesY * 0.1f) && spawnInfo.SpawnTileY < (Main.maxTilesY * 0.2f));
			bool oUnderSurface = (spawnInfo.SpawnTileY >= (Main.maxTilesY * 0.2f) && spawnInfo.SpawnTileY < (Main.maxTilesY * 0.3f));
			bool oUnderground = (spawnInfo.SpawnTileY >= (Main.maxTilesY * 0.3f) && spawnInfo.SpawnTileY < (Main.maxTilesY * 0.4f));
			bool oCavern = (spawnInfo.SpawnTileY >= (Main.maxTilesY * 0.4f) && spawnInfo.SpawnTileY < (Main.maxTilesY * 0.6f));
			bool oMagmaCavern = (spawnInfo.SpawnTileY >= (Main.maxTilesY * 0.6f) && spawnInfo.SpawnTileY < (Main.maxTilesY * 0.8f));
			bool oUnderworld = (spawnInfo.SpawnTileY >= (Main.maxTilesY * 0.8f));
			if (p.ZoneDungeon || p.ZoneMeteor) return 0;
			//if (Main.hardMode && oUnderworld && Main.rand.Next(1000)==1) return true;
			//if (Main.hardMode && Main.bloodMoon && oUnderworld && Main.rand.Next(100)==1) return true;
			//if (tsorcRevampWorld.SuperHardMode && Main.bloodMoon && oUnderworld && Main.rand.Next(30)==1) return true;
			//if (tsorcRevampWorld.SuperHardMode && oUnderworld && Main.rand.Next(35)==1) return true;

			if (spawnInfo.Player.ZoneUnderworldHeight)
			{
				if (!Main.dayTime && !Main.hardMode && !tsorcRevampWorld.SuperHardMode)
				{
					if (Main.rand.Next(21000) == 1) return 1;
					else if ((spawnInfo.SpawnTileX < Main.maxTilesX * 0.35f || spawnInfo.SpawnTileX > Main.maxTilesX * 0.75f) && Main.rand.Next(10000) == 1) return 1;
					return 0;
				}

				if (Main.hardMode)
				{
					bool hunterDowned = tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<NPCs.Bosses.TheHunter>());

					if (hunterDowned && !tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<AncientDemon>()) && Main.rand.Next(100) == 1) return 1;
					if (hunterDowned && Main.rand.Next(1000) == 1) return 1;
					if (hunterDowned && !Main.dayTime && Main.rand.Next(500) == 1) return 1;
					else if ((spawnInfo.SpawnTileX < Main.maxTilesX * 0.25f || spawnInfo.SpawnTileX > Main.maxTilesX * 0.75f) && tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<NPCs.Bosses.TheRage>()) && Main.rand.Next(500) == 1) return 1;
					return 0;
				}

				if (tsorcRevampWorld.SuperHardMode)
				{
					if (!tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<AncientDemon>()) && Main.rand.Next(100) == 1) return 1;
					if (Main.rand.Next(1000) == 1) return 1;
					else if (!Main.dayTime && Main.rand.Next(500) == 1) return 1;
					return 0;
				}
				return 0;
			}
			return 0;
		}
		#endregion

		int spawnedWerewolves = 0;
		public override void AI()
		{
			NPC.localAI[1]++;
			bool lineOfSight = Collision.CanHit(NPC.position, NPC.width, NPC.height, Main.player[NPC.target].position, Main.player[NPC.target].width, Main.player[NPC.target].height);
			tsorcRevampAIs.FighterAI(NPC, 1, 0.1f, canTeleport: true, lavaJumping: true);
			tsorcRevampAIs.SimpleProjectile(NPC, ref NPC.localAI[1], 90, ModContent.ProjectileType<Projectiles.Enemy.PoisonCrystalFire>(), poisonFireDamage, 10, Main.rand.Next(200) == 1, false, 2, 17);
			tsorcRevampAIs.SimpleProjectile(NPC, ref NPC.localAI[1], 90, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellGreatEnergyBeamBall>(), energyBeamDamage, 8, Main.rand.Next(200) == 1, false, 2, 17);
			tsorcRevampAIs.SimpleProjectile(NPC, ref NPC.localAI[1], 90, ModContent.ProjectileType<Projectiles.Enemy.FireBreath>(), fireBreathDamage, 8, Main.rand.Next(70) == 1, false, 2, 17, 0);
			tsorcRevampAIs.SimpleProjectile(NPC, ref NPC.localAI[1], 90, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellGreatFireballBall>(), greatFireballDamage, 8, lineOfSight && Main.rand.Next(200) == 1, false, 2, 17, 0);
			tsorcRevampAIs.SimpleProjectile(NPC, ref NPC.localAI[1], 90, ModContent.ProjectileType<Projectiles.Enemy.EnemyBlackFire>(), blackFireDamage, 13, lineOfSight && Main.rand.Next(150) == 1, false, 2, 17);
			tsorcRevampAIs.SimpleProjectile(NPC, ref NPC.localAI[1], 90, ModContent.ProjectileType<Projectiles.Enemy.EnemyGreatAttack>(), greatAttackDamage, 8, lineOfSight && Main.rand.Next(140) == 1, false, 2, 17);

			
			if ((spawnedWerewolves < 7) && Main.rand.Next(1000) == 1)
			{
				if (Main.netMode != NetmodeID.MultiplayerClient)
				{
					int Spawned = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.position.X + (NPC.width / 2), (int)NPC.position.Y + (NPC.height / 2), NPCID.Werewolf, 0);
					Main.npc[Spawned].velocity.Y = -8;
					spawnedWerewolves++;
					if (Main.netMode == NetmodeID.Server)
					{
						NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, Spawned, 0f, 0f, 0f, 0);
					}
				}
			}
		}

        public override void HitEffect(int hitDirection, double damage)
        {
			if (NPC.life <= 0)
			{
				Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Gores/Ancient Demon Gore 1").Type, 1f);
				Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Gores/Ancient Demon Gore 2").Type, 1f);
				Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Gores/Ancient Demon Gore 3").Type, 1f);
				Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Gores/Ancient Demon Gore 2").Type, 1f);
				Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Gores/Ancient Demon Gore 3").Type, 1f);
			}
		}
        public override void OnKill()
		{
			if(Main.rand.Next(99) < 50) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Potions.CrimsonPotion>(), 1);
			//if(Main.rand.Next(99) < 60)Item.NewItem(NPC.GetSource_Loot(), npc.getRect(), ModContent.ItemType<Items.Potions.Piercing>(), 1);
			if (Main.rand.Next(99) < 40) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Potions.StrengthPotion>(), 1);
			//if(Main.rand.Next(99) <= 60)Item.NewItem(NPC.GetSource_Loot(), npc.getRect(), ModContent.ItemType<Items.Potions.FiresoulPotion>(), 1);
			if (Main.rand.Next(99) < 20) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Potions.ShockwavePotion>(), 1);
			if (Main.rand.Next(99) < 40) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Potions.BattlefrontPotion>(), 1);
			if (Main.rand.Next(99) < 50) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Potions.AttractionPotion>(), 1);
			Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.IronskinPotion, 1);
			Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.ManaRegenerationPotion, 1);
			Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.ArcheryPotion, 1);
			if (Main.rand.Next(99) < 10) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Accessories.BandOfGreatCosmicPower>(), 1);
			Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.DarkSoul>(), 4000);
			if (Main.rand.Next(99) < 10) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Accessories.BarrierRing>(), 1);
			if (Main.rand.Next(99) < 10) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Accessories.EyeOfTheGods>(), 1);						
		}
	}
}