using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Bosses.SuperHardMode.Seath
{
    [AutoloadBossHead]
    class PrimordialCrystal : ModNPC
    {
        public override void SetDefaults()
        {
            npc.netAlways = true;
            npc.width = 64;
            npc.height = 64;
            drawOffsetY = 7;
            npc.aiStyle = 0;
            npc.knockBackResist = 0;
            npc.timeLeft = 22500;
            npc.damage = 1;
            npc.defense = 40;
            npc.HitSound = SoundID.NPCHit5;
            npc.DeathSound = SoundID.NPCDeath51;
            npc.lifeMax = 15000;
            npc.noGravity = true;
            npc.noTileCollide = false;
            npc.buffImmune[BuffID.Poisoned] = true;
            npc.buffImmune[BuffID.Confused] = true;
            npc.buffImmune[BuffID.OnFire] = true;
            npc.buffImmune[BuffID.CursedInferno] = true;
        }

        public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
        {
        }

        int? seathID;
        public override void AI()
        {
            npc.rotation += 0.02f;
            npc.velocity *= 0.95f;

            UsefulFunctions.DustRing(npc.Center, 50, DustID.MagicMirror);

            if (seathID == null || !Main.npc[seathID.Value].active) {
                seathID = UsefulFunctions.GetFirstNPC(ModContent.NPCType<NPCs.Bosses.SuperHardMode.Seath.SeathTheScalelessHead>());
            }
            else
            {
                Vector2 dustVel = UsefulFunctions.GenerateTargetingVector(npc.Center, Main.npc[seathID.Value].Center, 24);
                dustVel = dustVel.RotatedByRandom(MathHelper.ToRadians(12));
                Dust.NewDustDirect(npc.position, npc.width, npc.height, 67, dustVel.X, dustVel.Y, 250, Color.White, 2f).noGravity = true;
                Dust.NewDustDirect(npc.position, npc.width, npc.height, 68, dustVel.X, dustVel.Y, 250, Color.White, 2f).noGravity = true;
                Dust.NewDustDirect(npc.position, npc.width, npc.height, 234, dustVel.X, dustVel.Y, 250, Color.White, 2f).noGravity = true;
            }            
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color drawColor)
        {
            return true;
        }

        public override void NPCLoot()
        {
            for (int i = 0; i < 50; i++)
            {
                int dust;
                Vector2 vel = Main.rand.NextVector2Circular(20, 20);
                dust = Dust.NewDust(npc.Center, 30, 30, 234, vel.X, vel.Y, 240, default, 5f);
                Main.dust[dust].noGravity = true;
                dust = Dust.NewDust(npc.Center, 30, 30, 226, vel.X, vel.Y, 200, default, 3f);
                Main.dust[dust].noGravity = true;
            }

            for(int i = 0; i < Main.maxNPCs; i++)
            {
                if(Main.npc[i].type == ModContent.NPCType<SeathTheScalelessHead>() || Main.npc[i].type == ModContent.NPCType<SeathTheScalelessBody>() || Main.npc[i].type == ModContent.NPCType<SeathTheScalelessBody2>() || Main.npc[i].type == ModContent.NPCType<SeathTheScalelessBody3>() || Main.npc[i].type == ModContent.NPCType<SeathTheScalelessLegs>() || Main.npc[i].type == ModContent.NPCType<SeathTheScalelessTail>())
                {
                    if (Main.npc[i].active){
                        int dustCount = 15;
                        if (NPC.CountNPCS(ModContent.NPCType<PrimordialCrystal>()) > 1)
                        {
                            dustCount = 2;
                        }
                        for (int j = 0; j < dustCount; j++)
                        {
                            int dust;
                            Vector2 vel = Main.rand.NextVector2Circular(20, 20);
                            dust = Dust.NewDust(Main.npc[i].Center, 30, 30, 234, vel.X, vel.Y, 240, default, 5f);
                            Main.dust[dust].noGravity = true;
                            dust = Dust.NewDust(Main.npc[i].Center, 30, 30, 226, vel.X, vel.Y, 200, default, 3f);
                            Main.dust[dust].noGravity = true;
                        }
                    }
                }
            }
        }

        public override bool CheckActive()
        {
            return false;
        }

    }
}