using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Enemies.JungleWyvernJuvenile
{
	class JungleWyvernJuvenileBody3 : ModNPC
	{
		public override void SetDefaults()
		{
			npc.netAlways = true;
			npc.npcSlots = 1;
			npc.aiStyle = 6;
			npc.width = 30;
			npc.height = 30;
			npc.knockBackResist = 0f;
			npc.timeLeft = 1750;
			npc.damage = 30;
			npc.defense = 10;
			npc.HitSound = SoundID.NPCHit7;
			npc.DeathSound = SoundID.NPCDeath8;
			npc.lifeMax = 1500;
			npc.noGravity = true;
			npc.noTileCollide = true;
			npc.value = 1500;
			npc.scale = 0.7f;
			npc.buffImmune[BuffID.Poisoned] = true;
			npc.buffImmune[BuffID.OnFire] = true;
			npc.buffImmune[BuffID.Confused] = true;
			npc.buffImmune[BuffID.CursedInferno] = true;
		}

		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Jungle Wyvern Juvenile");
		}

		public override void AI()
		{

			if (!Main.npc[(int)npc.ai[1]].active)
			{
				npc.life = 0;
				npc.HitEffect(0, 10.0);
				NPCLoot();
				for (int num36 = 0; num36 < 10; num36++)
				{
					Color color = new Color();
					int dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 62, Main.rand.Next(-20, 20), Main.rand.Next(-20, 20), 100, color, 5f);
					Main.dust[dust].noGravity = false;
					dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 62, Main.rand.Next(-20, 20), Main.rand.Next(-20, 20), 100, color, 3f);
					Main.dust[dust].noGravity = false;
					dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 54, Main.rand.Next(-20, 20), Main.rand.Next(-20, 20), 100, color, 3f);
					Main.dust[dust].noGravity = false;
					dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 62, 0, 0, 100, Color.White, 5f);
					Main.dust[dust].noGravity = true;
					//npc.netUpdate = true; //new
				}

				npc.active = false;
			}
			if (npc.position.X > Main.npc[(int)npc.ai[1]].position.X)
			{
				npc.spriteDirection = 1;
			}
			if (npc.position.X < Main.npc[(int)npc.ai[1]].position.X)
			{
				npc.spriteDirection = -1;
			}
			if (Main.rand.Next(2) == 0)
			{
				int dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y + 10), npc.width, npc.height, 62, 0, 0, 100, Color.White, 1f);
				Main.dust[dust].noGravity = true;
			}
		}

		public override bool PreDraw(SpriteBatch spriteBatch, Color drawColor)
		{
			Vector2 origin = new Vector2(Main.npcTexture[npc.type].Width / 2, Main.npcTexture[npc.type].Height / Main.npcFrameCount[npc.type] / 2);
			Color alpha = Color.White;
			SpriteEffects effects = SpriteEffects.None;
			if (npc.spriteDirection == 1)
			{
				effects = SpriteEffects.FlipHorizontally;
			}
			spriteBatch.Draw(Main.npcTexture[npc.type], new Vector2(npc.position.X - Main.screenPosition.X + (float)(npc.width / 2) - (float)Main.npcTexture[npc.type].Width * npc.scale / 2f + origin.X * npc.scale, npc.position.Y - Main.screenPosition.Y + (float)npc.height - (float)Main.npcTexture[npc.type].Height * npc.scale / (float)Main.npcFrameCount[npc.type] + 4f + origin.Y * npc.scale + 36f), npc.frame, alpha, npc.rotation, origin, npc.scale, effects, 0f);
			npc.alpha = 255;
			return true;
		}
	}
}
