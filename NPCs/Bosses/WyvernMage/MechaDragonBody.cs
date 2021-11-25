using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Bosses.WyvernMage {
    class MechaDragonBody : ModNPC {
		public int Timer = -1000;

		public override void SetDefaults() {
			npc.netAlways = true;
			npc.npcSlots = 1;
			npc.aiStyle = 6;
			npc.width = 45;
			npc.height = 45;
			npc.knockBackResist = 0f;
			npc.timeLeft = 750;
			npc.damage = 70;
			npc.defense = 23;
			npc.HitSound = SoundID.NPCHit4;
			npc.DeathSound = SoundID.NPCDeath10;
			npc.lifeMax = 91000000;
			npc.boss = true;
			npc.noGravity = true;
			npc.noTileCollide = true;
			npc.behindTiles = true;
			npc.value = 25000;
			npc.buffImmune[BuffID.Poisoned] = true;
			npc.buffImmune[BuffID.OnFire] = true;
			npc.buffImmune[BuffID.Confused] = true;
			npc.buffImmune[BuffID.CursedInferno] = true;
		}

		public int CrystalFireDamage = 35;
		public override void SetStaticDefaults() {
			DisplayName.SetDefault("Wyvern Mage Disciple");
		}
		public override bool? DrawHealthBar(byte hbPosition, ref float scale, ref Vector2 position)
		{
			return false;
		}

		public override void ScaleExpertStats(int numPlayers, float bossLifeScale) {
			CrystalFireDamage = CrystalFireDamage / 2;
		}

		public override void AI() {


			//Generic Worm Part Code:
			tsorcRevampGlobalNPC.AIWorm(npc, ModContent.NPCType<MechaDragonHead>(), MechaDragonHead.bodyTypes, ModContent.NPCType<MechaDragonTail>(), 23, -1f, 12f, 0.13f, true, false);

			//Code unique to this body part:
			Timer++;
			if (!Main.npc[(int)npc.ai[1]].active)
			{
				npc.life = 0;
				NPCLoot();

				npc.HitEffect(0, 10.0);
				npc.active = false;
			}

			if (Timer >= 600)
			{
				if (Main.netMode != NetmodeID.Server)
				{
					float num48 = 6f;
					Vector2 vector8 = new Vector2(npc.position.X + (npc.width / 2), npc.position.Y + (npc.height / 2));
					float rotation = (float)Math.Atan2(vector8.Y - (Main.player[npc.target].position.Y + (Main.player[npc.target].height * 0.5f)), vector8.X - (Main.player[npc.target].position.X + (Main.player[npc.target].width * 0.5f)));
					rotation += Main.rand.Next(-50, 50) / 100;
					if (Main.netMode == NetmodeID.MultiplayerClient)
					{
						Projectile.NewProjectile(vector8.X, vector8.Y, (float)((Math.Cos(rotation) * num48) * -1), (float)((Math.Sin(rotation) * num48) * -1), ModContent.ProjectileType<Projectiles.Enemy.CrystalFire>(), CrystalFireDamage, 0f, Main.myPlayer);
					}
					Timer = -600 - Main.rand.Next(700);
				}

				//npc.netUpdate=true;
			}
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
		public override bool CheckActive()
		{
			return false;
		}
		public override void NPCLoot()
        {
			Vector2 vector8 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y + (npc.height / 2));
			Gore.NewGore(vector8, new Vector2((float)Main.rand.Next(-0, 1) * 0.2f, (float)Main.rand.Next(-0, 1) * 0.2f), Main.rand.Next(61, 64), 1f);
			Gore.NewGore(vector8, new Vector2((float)Main.rand.Next(-0, 1) * 0.2f, (float)Main.rand.Next(-0, 1) * 0.2f), Main.rand.Next(61, 64), 1f);
			Gore.NewGore(vector8, new Vector2((float)Main.rand.Next(-0, 1) * 0.2f, (float)Main.rand.Next(-0, 1) * 0.2f), Main.rand.Next(61, 64), 1f);
			Gore.NewGore(vector8, new Vector2((float)Main.rand.Next(-0, 1) * 0.2f, (float)Main.rand.Next(-0, 1) * 0.2f), Main.rand.Next(61, 64), 1f);
		}
	}
}
