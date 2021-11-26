using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Bosses.Okiku.SecondForm
{
	public class ShadowDragonBody3 : ModNPC
	{
		public int Timer = -1000;

		public override void SetDefaults()
		{
			npc.width = 22;
			npc.height = 22;
			npc.aiStyle = 6;
			npc.damage = 80;
			npc.defense = 20;
			npc.boss = true;
			npc.noGravity = true;
			npc.noTileCollide = true;
			npc.lifeMax = 91000000;
			npc.HitSound = SoundID.NPCHit1;
			npc.DeathSound = SoundID.NPCDeath8;
			npc.knockBackResist = 0f;
			drawOffsetY = 50;
			npc.dontCountMe = true;
			bodyTypes = new int[] {
			ModContent.NPCType<ShadowDragonBody>(), ModContent.NPCType<ShadowDragonBody>(), ModContent.NPCType<ShadowDragonBody>(),
			ModContent.NPCType<ShadowDragonBody>(), ModContent.NPCType<ShadowDragonLegs>(), ModContent.NPCType<ShadowDragonBody>(), ModContent.NPCType<ShadowDragonBody>(),
			ModContent.NPCType<ShadowDragonBody>(), ModContent.NPCType<ShadowDragonBody>(), ModContent.NPCType<ShadowDragonLegs>(), ModContent.NPCType<ShadowDragonBody>(),
			ModContent.NPCType<ShadowDragonBody>(), ModContent.NPCType<ShadowDragonBody>(), ModContent.NPCType<ShadowDragonBody>(), ModContent.NPCType<ShadowDragonLegs>(),
			ModContent.NPCType<ShadowDragonBody>(), ModContent.NPCType<ShadowDragonBody>(), ModContent.NPCType<ShadowDragonBody>(), ModContent.NPCType<ShadowDragonBody>(),
			ModContent.NPCType<ShadowDragonLegs>(), ModContent.NPCType<ShadowDragonBody>(), ModContent.NPCType<ShadowDragonBody2>(), ModContent.NPCType<ShadowDragonBody3>()
			};
		}
		public static int[] bodyTypes;


		int ObscureSawDamage = 45;
		public override void SetStaticDefaults()
		{
			base.DisplayName.SetDefault("Shadow Dragon");
		}
		public override bool? DrawHealthBar(byte hbPosition, ref float scale, ref Vector2 position)
		{
			return false;
		}
		public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
		{
			ObscureSawDamage = ObscureSawDamage / 2;
		}
        
        public override void AI()
		{
			tsorcRevampGlobalNPC.AIWorm(npc, ModContent.NPCType<ShadowDragonHead>(), bodyTypes, ModContent.NPCType<ShadowDragonTail>(), 25, 0.8f, 16f, 0.33f, true, false, true, false, false);

			if (Main.rand.Next(3) == 0)
			{
				int dust = Dust.NewDust(new Vector2(base.npc.position.X, base.npc.position.Y), base.npc.width, base.npc.height, 62, 0f, 0f, 100, Color.White, 2f);
				Main.dust[dust].noGravity = true;
			}

			if (Timer == -1000)
			{
				Timer = -Main.rand.Next(800);
			}
			Timer++;
			if (Timer >= 0)
			{
				float speed = 1f;
				Vector2 vector8 = new Vector2(npc.position.X + (npc.width / 2), npc.position.Y + (npc.height / 2));
				float rotation = (float)Math.Atan2(vector8.Y - (Main.player[npc.target].position.Y + Main.player[npc.target].height * 0.5f), vector8.X - (Main.player[npc.target].position.X + Main.player[npc.target].width * 0.5f));
				rotation += (Main.rand.Next(-50, 50) / 100);
				if (Main.netMode != NetmodeID.MultiplayerClient)
				{
					Projectile.NewProjectile(vector8.X, vector8.Y, (float)(Math.Cos(rotation) * speed * -1.0), (float)(Math.Sin(rotation) * speed * -1.0), mod.GetProjectile("ObscureSaw").projectile.type, ObscureSawDamage, 0f, Main.myPlayer);
				}
				Timer = -300 - Main.rand.Next(300);
			}
		}
	}
}
