using Terraria.Audio;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using tsorcRevamp.Items.Materials;
using Terraria.GameContent;
using Terraria.GameContent.ItemDropRules;

namespace tsorcRevamp.NPCs.Enemies.SuperHardMode
{
	public class OmnirsMassacre : ModNPC
	{
        float npcAcSPD = 1.5f; //How fast they accelerate.
        float npcSPD = 2.65f; //Max speed (Phase2: 50% slower, was 5.3f)

        float npcEnrAcSPD = 2.1f; //How fast they accelerate.
        float npcEnrSPD = 3.55f; //Max speed (Phase2: 50% slower, was 7.1f)

        public override void SetDefaults()
		{
			NPC.width = 140;
			NPC.height = 130;
			NPC.damage = 110;
			NPC.defense = 65;
			NPC.lifeMax = 15000;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath5;
            NPC.value = 400000;
			NPC.npcSlots = 5;
			NPC.knockBackResist = 0f;
			Main.npcFrameCount[NPC.type] = 16;
			AnimationType = 28;
			NPC.lavaImmune = true;
			NPC.buffImmune[BuffID.Venom] = true;
			NPC.buffImmune[BuffID.Confused] = true;
			NPC.buffImmune[BuffID.CursedInferno] = true;
			NPC.buffImmune[BuffID.OnFire] = true;

			// Phase 2: SmartFighter4AI movement + beast levers. minSurfaceWidth:4 (in AI) keeps this giant off
			// narrow ledges; NavSearchRadius enables A*; MaxJumpPower above the 8 default for a strong jump given
			// its size (NPC.gravity is read-only, so no true heavy "weighty" fall). Tune to taste.
			tsorcRevampGlobalNPC g = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
			g.NavSearchRadius = 24;
			g.MaxJumpPower = 10f;
			g.MaxJumpBoost = 6f;
		}
        //Spawns in the Underworld on hardmode.
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            if (tsorcRevampWorld.SuperHardMode)
            {
                if ((spawnInfo.Player.ZoneUnderworldHeight || (spawnInfo.Player.ZoneOverworldHeight && !Main.dayTime)) && Main.rand.NextBool(45))
                {
                    return 1;
                }
                else return 0;
            }
            else return 0;
        }
        public void teleport(bool pre)
		{
			if (Main.netMode != 2)
			{
				SoundEngine.PlaySound(SoundID.Item8, NPC.Center);
				for (int m = 0; m < 25; m++)
				{
					int dustID = Dust.NewDust(NPC.position, NPC.width, NPC.height, 6, 0, 0, 100, Color.White, 2f);
					Main.dust[dustID].noGravity = true;
					Main.dust[dustID].velocity = new Vector2(MathHelper.Lerp(-1f, 1f, (float)Main.rand.NextDouble()), MathHelper.Lerp(-1f, 1f, (float)Main.rand.NextDouble()));
					Main.dust[dustID].velocity *= 7f;
				}
			}
		}
		public override void AI()  //  warrior ai
		{
            bool enraged = (NPC.life < (float)NPC.lifeMax*.2f);  //  speed up at low life
			int shotRate = enraged?100:70;
			float accel=enraged? npcEnrAcSPD:npcAcSPD;  //  how fast it can speed up
			float topSpeed=enraged? npcEnrSPD:npcSPD;  //  max walking speed, also affects jump length
            tsorcRevampAIs.FighterAI(NPC, topSpeed: topSpeed, acceleration: accel, canPounce: false, canDodgeroll: false, canTeleport: true, minSurfaceWidth: 6, canWalkBackwards: true);
            Vector2 angle = Main.player[NPC.target].Center - NPC.Center;
            angle.Y = angle.Y - (Math.Abs(angle.X) * .1f);
            angle.X += (float)Main.rand.Next(-20, 21);
            angle.Y += (float)Main.rand.Next(-20, 21);
            angle.Normalize();
            if (NPC.lavaWet) NPC.velocity.Y-=2;
			float distance = NPC.Distance(Main.player[NPC.target].Center);
            #region shoot and walk
            if (Main.netMode != 1 && !Main.player[NPC.target].dead) // can generalize this section to moving+Projectile code // can generalize this section to moving+Projectile code
            {
                if (NPC.justHit)
                    NPC.ai[2] = 0f; // reset throw countdown when hit

                #region Charge
                if (NPC.velocity.Y == 0f && Main.rand.Next(550) == 1)
                {
                    Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
                    float rotation = (float)Math.Atan2(vector8.Y - (Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)), vector8.X - (Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)));
                    NPC.velocity.X = (float)(Math.Cos(rotation) * 7) * -1;
                    NPC.velocity.Y = (float)(Math.Sin(rotation) * 7) * -1;
                    NPC.ai[1] = 1f;
                    NPC.netUpdate = true;
                }
                #endregion
            }
            #endregion
            if (NPC.velocity.Y == 0f && Main.rand.Next(550) == 1)
            {
                Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
                float rotation = (float)Math.Atan2(vector8.Y - (Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)), vector8.X - (Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)));
                NPC.velocity.X = (float)(Math.Cos(rotation) * 7) * -1;
                NPC.velocity.Y = (float)(Math.Sin(rotation) * 7) * -1;
                NPC.localAI[3] = 1f;
                NPC.netUpdate = true;
            }
		}
		public override void OnKill()
		{
			Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("OmnirsTibianJuggernautGore1").Type, 1f);
			Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("OmnirsTibianJuggernautGore2").Type, 1f);
			Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("OmnirsTibianJuggernautGore3").Type, 1f);
			Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("OmnirsTibianJuggernautGore3").Type, 1f);
			Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("OmnirsTibianJuggernautGore3").Type, 1f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("OmnirsTibianJuggernautGore3").Type, 1f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("OmnirsTibianJuggernautGore2").Type, 1f);
        }
        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<BlueTitanite>(), 1, 2, 3));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<RedTitanite>(), 1, 2, 3));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<WhiteTitanite>(), 1, 2, 3));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<CursedSoul>(), 1, 4, 6));
        }
    }
}
