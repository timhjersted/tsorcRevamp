using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Enemies
{
	public class GhostOfTheForgottenWarrior : ModNPC
	{
		public override void SetDefaults()
		{
			npc.HitSound = SoundID.NPCHit1;
			npc.DeathSound = SoundID.NPCDeath1;
			npc.damage = 35;
			npc.lifeMax = 95;
			npc.defense = 16;
			npc.value = 350;
			npc.width = 20;
			npc.aiStyle = -1;
			npc.height = 40;
			npc.knockBackResist = 0.1f;
			banner = npc.type;
			bannerItem = ModContent.ItemType<Banners.GhostOfTheForgottenWarriorBanner>();

			animationType = NPCID.GoblinWarrior;
			Main.npcFrameCount[npc.type] = 16;

			if (Main.hardMode)
			{
				npc.lifeMax = 195;
				npc.defense = 20;
				npc.value = 450;
				npc.damage = 50;
			}

			if (tsorcRevampWorld.SuperHardMode)
			{
				npc.lifeMax = 1095;
				npc.defense = 70;
				npc.damage = 100;
				npc.value = 1000;
			}
		}
		public override void NPCLoot()
		{
			if (Main.rand.Next(10) == 0) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Weapons.Ranged.EphemeralThrowingSpear>(), Main.rand.Next(15, 26));
			if (Main.rand.Next(10) == 0) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Weapons.Magic.WallTome>());
		}

		#region Spawn
		public override float SpawnChance(NPCSpawnInfo spawnInfo)
		{
			float chance = 0f;

			if (!Main.hardMode && NPC.downedBoss3 && spawnInfo.player.ZoneDungeon)
			{
				return 0.25f;
			}
			else if (Main.hardMode && spawnInfo.player.ZoneDungeon)
			{
				return 0.12f;
			}
			else if (tsorcRevampWorld.SuperHardMode && spawnInfo.player.ZoneDungeon)
			{
				return 0.1f; //.05 is 3.85%
			}

			return chance;
		}

		#endregion


		float spearTimer = 0;

		public override void AI()  //  warrior ai
		{
			tsorcRevampAIs.FighterAI(npc, 1.5f, .04f, 0.2f, true, enragePercent: 0.2f, enrageTopSpeed: 2.4f);

			bool canFire = npc.Distance(Main.player[npc.target].Center) < 1600 && Collision.CanHitLine(npc.Center, 0, 0, Main.player[npc.target].Center, 0, 0);
			tsorcRevampAIs.SimpleProjectile(npc, ref spearTimer, 180, ModContent.ProjectileType<Projectiles.Enemy.BlackKnightSpear>(), 20, 8, canFire, true, 2, 17);

			if (npc.justHit)
			{
				spearTimer = 0f;
			}			
		}

		static Texture2D spearTexture;
		public override void PostDraw(SpriteBatch spriteBatch, Color drawColor)
		{
			if (spearTexture == null)
			{
				spearTexture = mod.GetTexture("Projectiles/Enemy/BlackKnightGhostSpear");
			}
			if (spearTimer >= 150)
			{
				Lighting.AddLight(npc.Center, Color.White.ToVector3() * 0.3f); //Pick a color, any color. The 0.5f tones down its intensity by 50%
				if (Main.rand.Next(3) == 1)
				{
					Dust.NewDust(npc.position, npc.width, npc.height, DustID.Smoke, npc.velocity.X, npc.velocity.Y);
				}
				SpriteEffects effects = npc.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
				if (npc.spriteDirection == -1)
				{
					spriteBatch.Draw(spearTexture, npc.Center - Main.screenPosition, new Rectangle(0, 0, spearTexture.Width, spearTexture.Height), drawColor, -MathHelper.PiOver2, new Vector2(8, 38), npc.scale, effects, 0); // facing left (8, 38 work)
				}
				else
				{
					spriteBatch.Draw(spearTexture, npc.Center - Main.screenPosition, new Rectangle(0, 0, spearTexture.Width, spearTexture.Height), drawColor, MathHelper.PiOver2, new Vector2(8, 38), npc.scale, effects, 0); // facing right, first value is height, higher number is higher
				}
			}
		}
		public override void HitEffect(int hitDirection, double damage)
		{
			for (int i = 0; i < 5; i++)
			{
				int dustType = 5;
				int dustIndex = Dust.NewDust(npc.position, npc.width, npc.height, dustType);
				Dust dust = Main.dust[dustIndex];
				dust.velocity.X = dust.velocity.X + Main.rand.Next(-50, 51) * 0.06f;
				dust.velocity.Y = dust.velocity.Y + Main.rand.Next(-50, 51) * 0.06f;
				dust.scale *= 1f + Main.rand.Next(-30, 31) * 0.01f;
				dust.noGravity = true;
			}
			if (npc.life <= 0)
			{
				for (int i = 0; i < 25; i++)
				{
					Dust.NewDust(npc.position, npc.width, npc.height, 5, Main.rand.Next(-3, 3), Main.rand.Next(-3, 3), 70, default(Color), 1f);
				}

				Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Wild Warrior Gore 1"), 1f);
				Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Wild Warrior Gore 2"), 1f);
				Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Wild Warrior Gore 3"), 1f);
				Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Wild Warrior Gore 2"), 1f);
				Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Wild Warrior Gore 3"), 1f);
				if (Main.rand.Next(10) == 0) Item.NewItem(npc.getRect(), ItemID.GoldenKey, 1);
			}
		}
	}
}