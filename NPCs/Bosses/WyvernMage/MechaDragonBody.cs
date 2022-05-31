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
			NPC.netAlways = true;
			NPC.npcSlots = 1;
			NPC.aiStyle = 6;
			NPC.width = 45;
			NPC.height = 45;
			NPC.knockBackResist = 0f;
			NPC.timeLeft = 750;
			NPC.damage = 70;
			NPC.defense = 23;
			NPC.HitSound = SoundID.NPCHit1;
			NPC.DeathSound = SoundID.NPCDeath10;
			NPC.lifeMax = 91000000;
			NPC.boss = true;
			NPC.noGravity = true;
			NPC.noTileCollide = true;
			NPC.behindTiles = true;
			NPC.value = 25000;
			NPC.buffImmune[BuffID.Poisoned] = true;
			NPC.buffImmune[BuffID.OnFire] = true;
			NPC.buffImmune[BuffID.Confused] = true;
			NPC.buffImmune[BuffID.CursedInferno] = true;
			bodyTypes = new int[] { ModContent.NPCType<MechaDragonBody>(), ModContent.NPCType<MechaDragonBody>(), ModContent.NPCType<MechaDragonLegs>(), ModContent.NPCType<MechaDragonBody>(),
				ModContent.NPCType<MechaDragonBody>(), ModContent.NPCType<MechaDragonBody>(), ModContent.NPCType<MechaDragonBody>(), ModContent.NPCType<MechaDragonLegs>(), ModContent.NPCType<MechaDragonBody>(),
				ModContent.NPCType<MechaDragonBody>(), ModContent.NPCType<MechaDragonBody>(), ModContent.NPCType<MechaDragonBody>(), ModContent.NPCType<MechaDragonLegs>(), ModContent.NPCType<MechaDragonBody>(),
				ModContent.NPCType<MechaDragonBody>(), ModContent.NPCType<MechaDragonBody>(), ModContent.NPCType<MechaDragonBody>(), ModContent.NPCType<MechaDragonLegs>(), ModContent.NPCType<MechaDragonBody>(),
				ModContent.NPCType<MechaDragonBody2>(), ModContent.NPCType<MechaDragonBody3>() };

		}
		public static int[] bodyTypes;

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
			tsorcRevampGlobalNPC.AIWorm(NPC, ModContent.NPCType<MechaDragonHead>(), bodyTypes, ModContent.NPCType<MechaDragonTail>(), 23, -1f, 12f, 0.13f, true, false);

			//Code unique to this body part:
			Timer++;
			if (!Main.npc[(int)NPC.ai[1]].active)
			{
				NPC.life = 0;
				NPCLoot();

				NPC.HitEffect(0, 10.0);
				NPC.active = false;
			}

			if (Timer >= 600)
			{
				if (Main.netMode != NetmodeID.Server)
				{
					float num48 = 6f;
					Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width / 2), NPC.position.Y + (NPC.height / 2));
					float rotation = (float)Math.Atan2(vector8.Y - (Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)), vector8.X - (Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)));
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

		public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) {
			Vector2 origin = new Vector2(Main.npcTexture[NPC.type].Width / 2, Main.npcTexture[NPC.type].Height / Main.npcFrameCount[NPC.type] / 2);
			Color alpha = Color.White;
			SpriteEffects effects = SpriteEffects.None;
			if (NPC.spriteDirection == 1) {
				effects = SpriteEffects.FlipHorizontally;
			}
			spriteBatch.Draw(Main.npcTexture[NPC.type], new Vector2(NPC.position.X - Main.screenPosition.X + (float)(NPC.width / 2) - (float)Main.npcTexture[NPC.type].Width * NPC.scale / 2f + origin.X * NPC.scale, NPC.position.Y - Main.screenPosition.Y + (float)NPC.height - (float)Main.npcTexture[NPC.type].Height * NPC.scale / (float)Main.npcFrameCount[NPC.type] + 4f + origin.Y * NPC.scale + 56f), NPC.frame, alpha, NPC.rotation, origin, NPC.scale, effects, 0f);
			NPC.alpha = 255;
			return true;
		}
		public override bool CheckActive()
		{
			return false;
		}
		public override void OnKill()
        {
			Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
			Gore.NewGore(NPC.GetSource_Death(), vector8, new Vector2((float)Main.rand.Next(-0, 1) * 0.2f, (float)Main.rand.Next(-0, 1) * 0.2f), Main.rand.Next(61, 64), 1f);
			Gore.NewGore(NPC.GetSource_Death(), vector8, new Vector2((float)Main.rand.Next(-0, 1) * 0.2f, (float)Main.rand.Next(-0, 1) * 0.2f), Main.rand.Next(61, 64), 1f);
			Gore.NewGore(NPC.GetSource_Death(), vector8, new Vector2((float)Main.rand.Next(-0, 1) * 0.2f, (float)Main.rand.Next(-0, 1) * 0.2f), Main.rand.Next(61, 64), 1f);
			Gore.NewGore(NPC.GetSource_Death(), vector8, new Vector2((float)Main.rand.Next(-0, 1) * 0.2f, (float)Main.rand.Next(-0, 1) * 0.2f), Main.rand.Next(61, 64), 1f);
		}
	}
}
