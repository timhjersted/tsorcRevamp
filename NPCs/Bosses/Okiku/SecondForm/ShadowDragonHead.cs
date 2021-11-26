using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Bosses.Okiku.SecondForm {
	public class ShadowDragonHead : ModNPC
	{
		//private bool initiate;

		public int TimerHeal;

		public float TimerAnim;

		public override void SetDefaults()
		{
			npc.width = 42;
			npc.height = 42;
			npc.aiStyle = 6;
			npc.damage = 90;
			npc.defense = 19;
			npc.boss = true;
			npc.noGravity = true;
			npc.noTileCollide = true;
			npc.lifeMax = 12600;
			npc.HitSound = SoundID.NPCHit1;
			npc.DeathSound = SoundID.NPCDeath1;
			npc.knockBackResist = 0f;
			despawnHandler = new NPCDespawnHandler(DustID.PurpleCrystalShard);

			drawOffsetY = 45;

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

		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Shadow Dragon");
		}

		public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
		{
		}

		NPCDespawnHandler despawnHandler;


		public override void AI()
		{
			despawnHandler.TargetAndDespawn(npc.whoAmI);
			tsorcRevampGlobalNPC.AIWorm(npc, ModContent.NPCType<ShadowDragonHead>(), bodyTypes, ModContent.NPCType<ShadowDragonTail>(), 25, 0f, 16f, 0.33f, true, false, true, false, false);

			if (Main.rand.Next(3) == 0)
			{
				int dust = Dust.NewDust(new Vector2(npc.position.X, npc.position.Y), npc.width, npc.height, 62, 0.8f, 0f, 100, Color.White, 2f);
				Main.dust[dust].noGravity = true;
			}			
		}

		
		public override bool CheckActive()
		{
			return false;
		}
        public override void NPCLoot() {
			Item.NewItem(npc.getRect(), ModContent.ItemType<Items.DarkSoul>(), 500);
        }
    }
}
