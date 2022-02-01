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
			Main.npcFrameCount[npc.type] = 16;
			animationType = 28;
			npc.height = 120;
			npc.width = 50;
			npc.damage = 55;
			npc.defense = 15;
			npc.lifeMax = 9000;
			npc.scale = 1;
			npc.HitSound = SoundID.NPCHit1;
			npc.DeathSound = SoundID.NPCDeath5;
			npc.value = 51000;
			npc.knockBackResist = 0;
			npc.lavaImmune = true;

			npc.buffImmune[BuffID.Confused] = true;
			npc.buffImmune[BuffID.OnFire] = true;
			npc.buffImmune[BuffID.Poisoned] = true;
			npc.buffImmune[BuffID.CursedInferno] = true;
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
			npc.damage = (int)(npc.damage / 2);
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
			Player p = spawnInfo.player;
			
			bool oSky = (spawnInfo.spawnTileY < (Main.maxTilesY * 0.1f));
			bool oSurface = (spawnInfo.spawnTileY >= (Main.maxTilesY * 0.1f) && spawnInfo.spawnTileY < (Main.maxTilesY * 0.2f));
			bool oUnderSurface = (spawnInfo.spawnTileY >= (Main.maxTilesY * 0.2f) && spawnInfo.spawnTileY < (Main.maxTilesY * 0.3f));
			bool oUnderground = (spawnInfo.spawnTileY >= (Main.maxTilesY * 0.3f) && spawnInfo.spawnTileY < (Main.maxTilesY * 0.4f));
			bool oCavern = (spawnInfo.spawnTileY >= (Main.maxTilesY * 0.4f) && spawnInfo.spawnTileY < (Main.maxTilesY * 0.6f));
			bool oMagmaCavern = (spawnInfo.spawnTileY >= (Main.maxTilesY * 0.6f) && spawnInfo.spawnTileY < (Main.maxTilesY * 0.8f));
			bool oUnderworld = (spawnInfo.spawnTileY >= (Main.maxTilesY * 0.8f));
			if (p.ZoneDungeon || p.ZoneMeteor) return 0;
			//if (Main.hardMode && oUnderworld && Main.rand.Next(1000)==1) return true;
			//if (Main.hardMode && Main.bloodMoon && oUnderworld && Main.rand.Next(100)==1) return true;
			//if (tsorcRevampWorld.SuperHardMode && Main.bloodMoon && oUnderworld && Main.rand.Next(30)==1) return true;
			//if (tsorcRevampWorld.SuperHardMode && oUnderworld && Main.rand.Next(35)==1) return true;

			if (spawnInfo.player.ZoneUnderworldHeight)
			{
				if (!Main.dayTime && !Main.hardMode && !tsorcRevampWorld.SuperHardMode)
				{
					if (Main.rand.Next(21000) == 1) return 1;
					else if ((spawnInfo.spawnTileX < Main.maxTilesX * 0.35f || spawnInfo.spawnTileX > Main.maxTilesX * 0.75f) && Main.rand.Next(10000) == 1) return 1;
					return 0;
				}

				if (Main.hardMode)
				{
					bool hunterDowned = tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<NPCs.Bosses.TheHunter>());

					if (hunterDowned && !tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<AncientDemon>()) && Main.rand.Next(100) == 1) return 1;
					if (hunterDowned && Main.rand.Next(1000) == 1) return 1;
					if (hunterDowned && !Main.dayTime && Main.rand.Next(500) == 1) return 1;
					else if ((spawnInfo.spawnTileX < Main.maxTilesX * 0.25f || spawnInfo.spawnTileX > Main.maxTilesX * 0.75f) && tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<NPCs.Bosses.TheRage>()) && Main.rand.Next(500) == 1) return 1;
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
			npc.localAI[1]++;
			bool lineOfSight = Collision.CanHit(npc.position, npc.width, npc.height, Main.player[npc.target].position, Main.player[npc.target].width, Main.player[npc.target].height);
			tsorcRevampAIs.FighterAI(npc, 1, 0.1f, canTeleport: true, lavaJumping: true);
			tsorcRevampAIs.SimpleProjectile(npc, ref npc.localAI[1], 90, ModContent.ProjectileType<Projectiles.Enemy.PoisonCrystalFire>(), poisonFireDamage, 10, Main.rand.Next(200) == 1, false, 2, 17);
			tsorcRevampAIs.SimpleProjectile(npc, ref npc.localAI[1], 90, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellGreatEnergyBeamBall>(), energyBeamDamage, 8, Main.rand.Next(200) == 1, false, 2, 17);
			tsorcRevampAIs.SimpleProjectile(npc, ref npc.localAI[1], 90, ModContent.ProjectileType<Projectiles.Enemy.FireBreath>(), fireBreathDamage, 8, Main.rand.Next(70) == 1, false, 2, 17, 0);
			tsorcRevampAIs.SimpleProjectile(npc, ref npc.localAI[1], 90, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellGreatFireballBall>(), greatFireballDamage, 8, lineOfSight && Main.rand.Next(200) == 1, false, 2, 17, 0);
			tsorcRevampAIs.SimpleProjectile(npc, ref npc.localAI[1], 90, ModContent.ProjectileType<Projectiles.Enemy.EnemyBlackFire>(), blackFireDamage, 13, lineOfSight && Main.rand.Next(150) == 1, false, 2, 17);
			tsorcRevampAIs.SimpleProjectile(npc, ref npc.localAI[1], 90, ModContent.ProjectileType<Projectiles.Enemy.EnemyGreatAttack>(), greatAttackDamage, 8, lineOfSight && Main.rand.Next(140) == 1, false, 2, 17);

			
			if ((spawnedWerewolves < 7) && Main.rand.Next(1000) == 1)
			{
				if (Main.netMode != NetmodeID.MultiplayerClient)
				{
					int Spawned = NPC.NewNPC((int)npc.position.X + (npc.width / 2), (int)npc.position.Y + (npc.height / 2), NPCID.Werewolf, 0);
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
			if (npc.life <= 0)
			{
				Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Ancient Demon Gore 1"), 1f);
				Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Ancient Demon Gore 2"), 1f);
				Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Ancient Demon Gore 3"), 1f);
				Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Ancient Demon Gore 2"), 1f);
				Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Ancient Demon Gore 3"), 1f);
			}
		}
        public override void NPCLoot()
		{
			if(Main.rand.Next(99) < 50) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Potions.CrimsonPotion>(), 1);
			//if(Main.rand.Next(99) < 60)Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Potions.Piercing>(), 1);
			if (Main.rand.Next(99) < 40) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Potions.StrengthPotion>(), 1);
			//if(Main.rand.Next(99) <= 60)Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Potions.FiresoulPotion>(), 1);
			if (Main.rand.Next(99) < 20) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Potions.ShockwavePotion>(), 1);
			if (Main.rand.Next(99) < 40) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Potions.BattlefrontPotion>(), 1);
			if (Main.rand.Next(99) < 50) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Potions.AttractionPotion>(), 1);
			Item.NewItem(npc.getRect(), ItemID.IronskinPotion, 1);
			Item.NewItem(npc.getRect(), ItemID.ManaRegenerationPotion, 1);
			Item.NewItem(npc.getRect(), ItemID.ArcheryPotion, 1);
			if (Main.rand.Next(99) < 10) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Accessories.BandOfGreatCosmicPower>(), 1);
			Item.NewItem(npc.getRect(), ModContent.ItemType<Items.DarkSoul>(), 4000);
			if (Main.rand.Next(99) < 10) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Accessories.BarrierRing>(), 1);
			if (Main.rand.Next(99) < 10) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Accessories.EyeOfTheGods>(), 1);						
		}
	}
}