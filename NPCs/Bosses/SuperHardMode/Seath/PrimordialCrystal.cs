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
            drawOffsetY = 60;
            npc.aiStyle = 0;
            npc.knockBackResist = 0;
            npc.timeLeft = 22500;
            npc.damage = 0;
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
            npc.lifeMax = (int)(npc.lifeMax / 2);
        }

        public override void AI()
        {
            npc.rotation += 0.02f;
            npc.velocity *= 0.95f;           

            for (int i = 0; i < 10; i++)
            {
                int dustRadius = 80;
                int dustSpeed = 8;
                Vector2 dustOffset = Main.rand.NextVector2CircularEdge(1, 1);
                Vector2 dustVel;
                if (Main.rand.NextBool())
                {
                    dustVel = dustOffset.RotatedBy(MathHelper.ToRadians(90));
                }
                else
                {
                    dustVel = dustOffset.RotatedBy(MathHelper.ToRadians(-90));
                }
                Dust.NewDustPerfect(npc.Center + (dustOffset * dustRadius), 234, dustVel * dustSpeed, 250, Color.White, 1.0f).noGravity = true;

                dustVel = new Vector2(dustSpeed, 0);
                dustVel = dustVel.RotatedBy(MathHelper.ToRadians(Main.rand.NextFloat(45)) + npc.rotation);
                if (Main.rand.NextBool())
                {
                    dustVel = dustVel.RotatedBy(MathHelper.ToRadians(180));
                }
                Dust.NewDustPerfect(npc.Center, 226, dustVel, 250, Color.White, .5f).noGravity = true;                
            }

            for(int i = 0; i < 2; i++)
            {
                Vector2 dustVel = (Main.npc[(int)npc.ai[0]].Center - npc.position);
                float length = dustVel.Length();
                float angle = dustVel.ToRotation();
                dustVel = new Vector2(length / 8, 0).RotatedBy(angle);
                Dust.NewDustPerfect(npc.Center, 67, dustVel.RotatedByRandom(MathHelper.ToRadians(4)), 250, Color.White, 2f).noGravity = true;
                Dust.NewDustPerfect(npc.Center, 68, dustVel.RotatedByRandom(MathHelper.ToRadians(8)), 250, Color.White, 2f).noGravity = true;
                Dust.NewDustPerfect(npc.Center, 234, dustVel.RotatedByRandom(MathHelper.ToRadians(12)), 250, Color.White, 2f).noGravity = true;
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color drawColor)
        {
            return false;
        }

        public override void NPCLoot()
        {
            for (int i = 0; i < 50; i++)
            {
                Vector2 vel = Main.rand.NextVector2Circular(10, 10);
                int dust;
                vel = Main.rand.NextVector2Circular(20, 20);
                dust = Dust.NewDust(npc.Center, 30, 30, 234, vel.X, vel.Y, 240, default, 5f);
                Main.dust[dust].noGravity = true;
                dust = Dust.NewDust(npc.Center, 30, 30, 226, vel.X, vel.Y, 200, default, 3f);
                Main.dust[dust].noGravity = true;
            }
        }

        public override bool CheckActive()
        {
            return false;
        }

    }
}