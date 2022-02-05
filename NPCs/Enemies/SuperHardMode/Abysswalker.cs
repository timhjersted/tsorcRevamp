using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Enemies.SuperHardMode
{
	class Abysswalker : ModNPC
	{
		public override void SetDefaults()
		{
			npc.npcSlots = 5;
			Main.npcFrameCount[npc.type] = 15;
			animationType = 21;
			npc.knockBackResist = 0;
			npc.aiStyle = 3;
			npc.damage = 105;
			npc.defense = 72;
			npc.height = 40;
			npc.lifeMax = 7000;
			npc.scale = 1;
			npc.HitSound = SoundID.NPCHit1;
			npc.DeathSound = SoundID.NPCDeath1;
			npc.value = 12500;
			npc.width = 18;
			banner = npc.type;
			bannerItem = ModContent.ItemType<Banners.AbysswalkerBanner>();
		}


		int poisonBallDamage = 27;
		int stormBallDamage = 30;

		public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
		{
			npc.lifeMax = (int)(npc.lifeMax / 2);
			npc.damage = (int)(npc.damage / 2);
			poisonBallDamage = (int)(poisonBallDamage * tsorcRevampWorld.SubtleSHMScale);
			stormBallDamage = (int)(stormBallDamage * tsorcRevampWorld.SubtleSHMScale);
		}


		//Spawns in the Jungle Underground and in the Cavern.
		#region Spawn
		public override float SpawnChance(NPCSpawnInfo spawnInfo)
		{
			Player player = spawnInfo.player;			

			// these are all the regular stuff you get , now lets see......
			float chance = 0;

			if ((player.ZoneMeteor || player.ZoneJungle) && tsorcRevampWorld.SuperHardMode && !player.ZoneDungeon && !(player.ZoneCorrupt || player.ZoneCrimson))
			{
				chance = 0.25f;
			}
			if (player.ZoneDirtLayerHeight)
			{
				chance *= 1.5f;
			}
			if (player.ZoneRockLayerHeight)
			{
				chance *= 1.5f;
			}
			if (Main.bloodMoon)
			{
				chance *= 2;
			}

			return chance;
		}
		#endregion

		float poisonStrikeTimer = 0;
		float poisonStormTimer = 0;
		public override void AI()
		{
			tsorcRevampAIs.FighterAI(npc, 2f, 0.05f, 0.2f, true, enragePercent: 0.3f, enrageTopSpeed: 3);

			bool clearLineofSight = Collision.CanHit(npc.position, npc.width, npc.height, Main.player[npc.target].position, Main.player[npc.target].width, Main.player[npc.target].height);

			tsorcRevampAIs.SimpleProjectile(npc, ref poisonStrikeTimer, 120, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellAbyssPoisonStrikeBall>(), poisonBallDamage, 9, clearLineofSight, true, 2, 17);
			tsorcRevampAIs.SimpleProjectile(npc, ref poisonStormTimer, 180, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellAbyssStormBall>(), stormBallDamage, 0, clearLineofSight, true, 2, 17);

			if (poisonStrikeTimer >= 60)
			{
				Dust.NewDustDirect(npc.position, npc.width, npc.height, DustID.Ichor, 0, 0).noGravity = true;
			}
			if (poisonStormTimer >= 90)
			{
				UsefulFunctions.DustRing(npc.Center, 32, DustID.BlueCrystalShard, 12, 4);
				Lighting.AddLight(npc.Center, Color.Orange.ToVector3() * 5);
				if (Collision.CanHit(npc.position, npc.width, npc.height, Main.player[npc.target].position, Main.player[npc.target].width, Main.player[npc.target].height))
				{
					npc.velocity = Vector2.Zero;
				}
			}

			//Transparency. Higher alpha = more invisible
			if (npc.justHit)
			{
				npc.alpha = 0;
			}
			if (Main.rand.Next(200) == 1)
			{
				npc.alpha = 0;
			}
			if (Main.rand.Next(50) == 1)
			{
				npc.alpha = 210;
			}
			if (Main.rand.Next(250) == 1)
			{
				npc.life += 5;
				if (npc.life > npc.lifeMax) npc.life = npc.lifeMax;
				npc.netUpdate = true;
			}
		}

		#region Gore
		public override void NPCLoot()
		{
			Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Dworc Voodoomaster Gore 1"), 1f);
			Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Dworc Gore 2"), 1f);
			Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Dworc Gore 3"), 1f);
			Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Dworc Gore 2"), 1f);
			Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Dworc Gore 3"), 1f);

			if (Main.rand.Next(99) < 10) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.FlameOfTheAbyss>(), 4 + Main.rand.Next(3));
		}
		#endregion


	}
}