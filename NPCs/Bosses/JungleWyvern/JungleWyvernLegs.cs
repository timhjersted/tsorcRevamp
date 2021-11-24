using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Bosses.JungleWyvern {
	class JungleWyvernLegs : ModNPC {

		public override void SetDefaults() {
			npc.netAlways = true;
			npc.boss = true;
			npc.npcSlots = 1;
			npc.aiStyle = 6;
			npc.width = 45;
			npc.height = 45;
			npc.knockBackResist = 0f;
			npc.timeLeft = 1750;
			npc.damage = 38;
			npc.defense = 7;
			npc.HitSound = SoundID.NPCHit7;
			npc.DeathSound = SoundID.NPCDeath8;
			npc.lifeMax = 24000;
			npc.noGravity = true;
			npc.noTileCollide = true;
			npc.value = 70000;
			npc.buffImmune[BuffID.Poisoned] = true;
			npc.buffImmune[BuffID.OnFire] = true;
			npc.buffImmune[BuffID.Confused] = true;
			npc.buffImmune[BuffID.CursedInferno] = true;
		}

		public override void SetStaticDefaults() {
			DisplayName.SetDefault("Ancient Jungle Wyvern");
		}

		public override bool? DrawHealthBar(byte hbPosition, ref float scale, ref Vector2 position)
		{
			return false;
		}
				
		public int PoisonFlamesDamage = 45;
		public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
		{

		}
		public override void AI() {
			npc.TargetClosest();
			if (!Main.npc[(int)npc.ai[1]].active) {
				npc.life = 0;
				npc.HitEffect(0, 10.0);
				NPCLoot();
				for (int num36 = 0; num36 < 50; num36++) {
					Color color = new Color();
					int dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 62, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, color, 10f);
					Main.dust[dust].noGravity = false;
					dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 62, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, color, 6f);
					Main.dust[dust].noGravity = false;
					dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 54, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, color, 6f);
					Main.dust[dust].noGravity = false;
					dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 62, 0, 0, 100, Color.White, 10.0f);
					Main.dust[dust].noGravity = true;
					//npc.netUpdate = true; //new
				}

				npc.active = false;
			}
			if (npc.position.X > Main.npc[(int)npc.ai[1]].position.X) {
				npc.spriteDirection = 1;
			}
			if (npc.position.X < Main.npc[(int)npc.ai[1]].position.X) {
				npc.spriteDirection = -1;
			}
			int mainDust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y + 10), npc.width, npc.height, 62, 0, 0, 100, default, 1.0f);
			Main.dust[mainDust].noGravity = true;
		}
		public override void OnHitByItem(Player player, Item item, int damage, float knockback, bool crit)
		{
			damage *= 2;
			base.OnHitByItem(player, item, damage, knockback, crit);
		}

		public override bool PreDraw(SpriteBatch spriteBatch, Color drawColor) {
			Vector2 origin = new Vector2(Main.npcTexture[npc.type].Width / 2, Main.npcTexture[npc.type].Height / Main.npcFrameCount[npc.type] / 2);
			Color alpha = Color.White;
			SpriteEffects effects = SpriteEffects.None;
			if (npc.spriteDirection == 1) {
				effects = SpriteEffects.FlipHorizontally;
			}
			spriteBatch.Draw(Main.npcTexture[npc.type], new Vector2(npc.position.X - Main.screenPosition.X + (float)(npc.width / 2) - (float)Main.npcTexture[npc.type].Width * npc.scale / 2f + origin.X * npc.scale, npc.position.Y - Main.screenPosition.Y + (float)npc.height - (float)Main.npcTexture[npc.type].Height * npc.scale / (float)Main.npcFrameCount[npc.type] + 4f + origin.Y * npc.scale + 56f), npc.frame, alpha, npc.rotation, origin, npc.scale, effects, 0f);
			npc.alpha = 255;
			return true;
		}
	}
}
