using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items;

namespace tsorcRevamp.NPCs.Bosses.WyvernMage {
	[AutoloadBossHead]
	class MechaDragonHead : ModNPC {

		//Bit of a long boi, but it still works.
		public static int[] bodyTypes = new int[] { ModContent.NPCType<MechaDragonBody>(), ModContent.NPCType<MechaDragonBody>(), ModContent.NPCType<MechaDragonLegs>(), ModContent.NPCType<MechaDragonBody>(), ModContent.NPCType<MechaDragonBody>(), ModContent.NPCType<MechaDragonBody>(), ModContent.NPCType<MechaDragonBody>(), ModContent.NPCType<MechaDragonLegs>(), ModContent.NPCType<MechaDragonBody>(), ModContent.NPCType<MechaDragonBody>(), ModContent.NPCType<MechaDragonBody>(), ModContent.NPCType<MechaDragonBody>(), ModContent.NPCType<MechaDragonLegs>(), ModContent.NPCType<MechaDragonBody>(), ModContent.NPCType<MechaDragonBody>(), ModContent.NPCType<MechaDragonBody>(), ModContent.NPCType<MechaDragonBody>(), ModContent.NPCType<MechaDragonLegs>(), ModContent.NPCType<MechaDragonBody>(), ModContent.NPCType<MechaDragonBody2>(), ModContent.NPCType<MechaDragonBody3>() };

		public override void SetStaticDefaults() {
            DisplayName.SetDefault("Wyvern Mage Disciple");
        }
        public override void SetDefaults() {
			npc.aiStyle = 6;
            npc.netAlways = true;
            npc.npcSlots = 1;
            npc.width = 45;
            npc.height = 45;
            npc.timeLeft = 22750;
            npc.damage = 90;
            npc.defense = 10;
            npc.HitSound = SoundID.NPCHit4;
            npc.DeathSound = SoundID.NPCDeath10;
            npc.lifeMax = 91000;
            npc.knockBackResist = 0f;
            npc.noGravity = true;
            npc.noTileCollide = true;
			npc.behindTiles = true;
			npc.boss = true;
            npc.value = 25000;
            npc.buffImmune[BuffID.Poisoned] = true;
            npc.buffImmune[BuffID.OnFire] = true;
            npc.buffImmune[BuffID.Confused] = true;
            npc.buffImmune[BuffID.CursedInferno] = true;
			despawnHandler = new NPCDespawnHandler(DustID.OrangeTorch);
		}

		/**public override bool? DrawHealthBar(byte hbPosition, ref float scale, ref Vector2 position)
		{
			return head ? (bool?)null : false;
		}**/

		public override void ScaleExpertStats(int numPlayers, float bossLifeScale) {
		}

		NPCDespawnHandler despawnHandler;
		public override void AI()
		{
			despawnHandler.TargetAndDespawn(npc.whoAmI);

			//Generic Worm Part Code:
			npc.behindTiles = true;
			tsorcRevampGlobalNPC.AIWorm(npc, ModContent.NPCType<MechaDragonHead>(), bodyTypes, ModContent.NPCType<MechaDragonTail>(), 23, -1f, 12f, 0.13f, true, false, true, false, false); //3f was 6f

			//Code unique to this body part:
			Color color = new Color();
			int dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 62, 0, 0, 100, color, 1.0f);
			Main.dust[dust].noGravity = true;
		}

		public override bool PreDraw(SpriteBatch spriteBatch, Color drawColor) {
			Vector2 origin = new Vector2(Main.npcTexture[npc.type].Width / 2, Main.npcTexture[npc.type].Height / Main.npcFrameCount[npc.type] / 2);
			Color alpha = Color.White;
			SpriteEffects effects = SpriteEffects.None;
			if (npc.spriteDirection == 1) {
				effects = SpriteEffects.FlipHorizontally;
			}
			spriteBatch.Draw(Main.npcTexture[npc.type], new Vector2(npc.position.X - Main.screenPosition.X + npc.width / 2 - (float)Main.npcTexture[npc.type].Width * npc.scale / 2f + origin.X * npc.scale, npc.position.Y - Main.screenPosition.Y + (float)npc.height - (float)Main.npcTexture[npc.type].Height * npc.scale / (float)Main.npcFrameCount[npc.type] + 4f + origin.Y * npc.scale + 56f), npc.frame, alpha, npc.rotation, origin, npc.scale, effects, 0f);
			npc.alpha = 255;
			return true;
		}

		private static int ClosestSegment(NPC head, params int[] segmentIDs) { 
			List<int> segmentIDList = new List<int>(segmentIDs);
			Vector2 targetPos = Main.player[head.target].Center;
			int closestSegment = head.whoAmI; //head is default, updates later
			float minDist = 1000000f; //arbitrarily large, updates later
			for (int i = 0; i < Main.npc.Length; i++) { //iterate through every NPC
				NPC npc = Main.npc[i];
				if (npc != null && npc.active && segmentIDList.Contains(npc.type)) { //if the npc is part of the wyvern
					float targetDist = (npc.Center - targetPos).Length();
					if (targetDist < minDist) { //if we're closer than the previously closer segment (or closer than 1,000,000 if it's the first iteration, so always)
						minDist = targetDist; //update minDist. future iterations will compare against the updated value
						closestSegment = i; //and set closestSegment to the whoAmI of the closest segment
                    }
                }
            }
			return closestSegment; //the whoAmI of the closest segment
		}

		public override bool SpecialNPCLoot() {
			int closestSegmentID = ClosestSegment(npc, ModContent.NPCType<MechaDragonBody>(), ModContent.NPCType<MechaDragonBody2>(), ModContent.NPCType<MechaDragonBody3>(), ModContent.NPCType<MechaDragonLegs>(), ModContent.NPCType<MechaDragonTail>());
			npc.position = Main.npc[closestSegmentID].position; //teleport the head to the location of the closest segment before running npcloot
			return false;
		}
		public override bool CheckActive()
		{
			return false;
		}

		public override void BossLoot(ref string name, ref int potionType)
		{
			potionType = ItemID.GreaterHealingPotion;
		}

		public override void NPCLoot() {

			//Kind of like EoW, it always drops this many extra souls whether it's been killed or not.
			Item.NewItem(npc.getRect(), ModContent.ItemType<DarkSoul>(),900);
			
			if (!(tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<MechaDragonHead>())))
			{ //If the boss has not yet been killed
				Item.NewItem(npc.getRect(), ModContent.ItemType<DarkSoul>(), 5000); //Then drop the souls
			}
		}
    }
}
