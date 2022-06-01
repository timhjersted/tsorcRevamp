using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Enemies.SuperHardMode
{
	class AncientDemonOfTheAbyss : ModNPC
	{
        public override void SetStaticDefaults()
        {
			DisplayName.SetDefault("Ancient Demon of the Abyss");
		}
        public override void SetDefaults()
		{
			NPC.npcSlots = 6;
			Main.npcFrameCount[NPC.type] = 16;
			animationType = 28;
			NPC.height = 120;
			NPC.width = 50;
			NPC.damage = 120;
			NPC.defense = 70;
			NPC.lifeMax = 30000;
			NPC.HitSound = SoundID.NPCHit1;
			NPC.DeathSound = SoundID.NPCDeath5;

			NPC.value = 28750;
			NPC.knockBackResist = 0;
			NPC.lavaImmune = true;
			banner = NPC.type;
			bannerItem = ModContent.ItemType<Banners.AncientDemonOfTheAbyssBanner>();

			NPC.buffImmune[BuffID.Poisoned] = true;
			NPC.buffImmune[BuffID.Confused] = true;
			NPC.buffImmune[BuffID.CursedInferno] = true;
			NPC.buffImmune[BuffID.OnFire] = true;
		}

		int poisonFireDamage = 20;
		int energyBeamDamage = 23;
		int fireBreathDamage = 25;
		int greatFireballDamage = 20;
		int blackFireDamage = 25;
		int greatAttackDamage = 37;

		public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
		{
			NPC.lifeMax = (int)(NPC.lifeMax / 2);
			NPC.damage = (int)(NPC.damage / 2);
			poisonFireDamage = (int)(poisonFireDamage * tsorcRevampWorld.SubtleSHMScale);
			energyBeamDamage = (int)(energyBeamDamage * tsorcRevampWorld.SubtleSHMScale);
			fireBreathDamage = (int)(fireBreathDamage * tsorcRevampWorld.SubtleSHMScale);
			greatFireballDamage = (int)(greatFireballDamage * tsorcRevampWorld.SubtleSHMScale);
			blackFireDamage = (int)(blackFireDamage * tsorcRevampWorld.SubtleSHMScale);
			greatAttackDamage = (int)(greatAttackDamage * tsorcRevampWorld.SubtleSHMScale);
		}

		//Spawns in Lower Cavern into the Underworld. Spawns more under 2.5/10th and again after 7.5/10th (Length). Spawns more in Hardmode. Will not spawn if there are more than 2 Town NPCs nearby (or if a Blood Moon).

		#region Spawn
		public override float SpawnChance(NPCSpawnInfo spawnInfo)
		{
			bool oMagmaCavern = (spawnInfo.Player.position.Y >= (Main.maxTilesY * 0.6f) && !spawnInfo.Player.ZoneUnderworldHeight);
			bool BeforeThreeAfterSeven = (spawnInfo.Player.position.X < Main.maxTilesX * 0.3f) || (spawnInfo.Player.position.X > Main.maxTilesX * 0.7f); //Before 3/10ths or after 7/10ths width

			float chance = 0;
			if (tsorcRevampWorld.SuperHardMode)
			{
				if (spawnInfo.Player.ZoneUnderworldHeight)
				{
					chance = 0.1f;

					if (BeforeThreeAfterSeven)
					{
						chance *= 2;
					}
				}
				if (oMagmaCavern)
				{
					chance = 0.003f;
				}
			}

            if (Main.bloodMoon)
            {
				chance *= 2;
            }

			return chance;
		}
		#endregion

		int intspawnedSpirits = 0;
		public override void AI()
		{
			tsorcRevampAIs.FighterAI(NPC, 3, 0.1f, canTeleport: true, lavaJumping: true, enragePercent: 0.2f, enrageTopSpeed: 6);

			NPC.localAI[1]++;
			bool lineOfSight = Collision.CanHit(NPC.position, NPC.width, NPC.height, Main.player[NPC.target].position, Main.player[NPC.target].width, Main.player[NPC.target].height);
			tsorcRevampAIs.SimpleProjectile(NPC, ref NPC.localAI[1], 90, ModContent.ProjectileType<Projectiles.Enemy.PoisonCrystalFire>(), poisonFireDamage, 10, Main.rand.Next(200) == 1, false, 2, 17);
			tsorcRevampAIs.SimpleProjectile(NPC, ref NPC.localAI[1], 90, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellGreatEnergyBeamBall>(), energyBeamDamage, 8, Main.rand.Next(200) == 1, false, 2, 17);
			tsorcRevampAIs.SimpleProjectile(NPC, ref NPC.localAI[1], 90, ModContent.ProjectileType<Projectiles.Enemy.FireBreath>(), fireBreathDamage, 8, Main.rand.Next(70) == 1, false, 2, 17, 0);
			tsorcRevampAIs.SimpleProjectile(NPC, ref NPC.localAI[1], 90, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellGreatFireballBall>(), greatFireballDamage, 8, lineOfSight && Main.rand.Next(200) == 1, false, 2, 17, 0);
			tsorcRevampAIs.SimpleProjectile(NPC, ref NPC.localAI[1], 90, ModContent.ProjectileType<Projectiles.Enemy.EnemyBlackFire>(), blackFireDamage, 13, lineOfSight && Main.rand.Next(150) == 1, false, 2, 17);
			tsorcRevampAIs.SimpleProjectile(NPC, ref NPC.localAI[1], 90, ModContent.ProjectileType<Projectiles.Enemy.EnemyGreatAttack>(), greatAttackDamage, 8, lineOfSight && Main.rand.Next(140) == 1, false, 2, 17);


			if ((intspawnedSpirits < 7) && Main.rand.Next(1000) == 1)
			{
				if (Main.netMode != NetmodeID.MultiplayerClient)
				{
					int Spawned = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X, (int)NPC.Center.Y, ModContent.NPCType<NPCs.Enemies.CrazedDemonSpirit>(), 0);
					Main.npc[Spawned].velocity.Y = -8;
					intspawnedSpirits++;
					if (Main.netMode == NetmodeID.Server)
					{
						NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, Spawned, 0f, 0f, 0f, 0);
					}
				}
			}
		}

		#region Gore
		public override void OnKill()
		{
			if (NPC.life <= 0)
			{
				Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Gores/Ancient Demon Gore 1").Type, 1f);
				Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Gores/Ancient Demon Gore 2").Type, 1f);
				Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Gores/Ancient Demon Gore 3").Type, 1f);
				Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Gores/Ancient Demon Gore 2").Type, 1f);
				Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Gores/Ancient Demon Gore 3").Type, 1f);

				Item.NewItem(NPC.getRect(), ModContent.ItemType<Items.Humanity>(), 1 + Main.rand.Next(1));
				if (Main.rand.Next(99) < 10) Item.NewItem(NPC.getRect(), ModContent.ItemType<Items.Humanity>(), 10);
				if (Main.rand.Next(99) < 2) Item.NewItem(NPC.getRect(), ModContent.ItemType<Items.Weapons.Melee.Ragnarok>(), 1);
			}
		}
		#endregion

	}
}