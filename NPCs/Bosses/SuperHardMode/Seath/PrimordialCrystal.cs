using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
            NPC.netAlways = true;
            NPC.width = 64;
            NPC.height = 64;
            DrawOffsetY = 7;
            NPC.aiStyle = 0;
            NPC.knockBackResist = 0;
            NPC.timeLeft = 22500;
            NPC.damage = 1;
            NPC.defense = 40;
            NPC.HitSound = SoundID.NPCHit5;
            NPC.DeathSound = SoundID.NPCDeath51;
            NPC.lifeMax = 15000;
            NPC.noGravity = true;
            NPC.noTileCollide = false;
            NPC.buffImmune[BuffID.Poisoned] = true;
            NPC.buffImmune[BuffID.Confused] = true;
            NPC.buffImmune[BuffID.OnFire] = true;
            NPC.buffImmune[BuffID.CursedInferno] = true;
        }

        public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
        {
        }

        int? seathID;
        public override void AI()
        {
            NPC.rotation += 0.02f;
            NPC.velocity *= 0.95f;

            UsefulFunctions.DustRing(NPC.Center, 50, DustID.MagicMirror);

            if (seathID == null || !Main.npc[seathID.Value].active)
            {
                seathID = UsefulFunctions.GetFirstNPC(ModContent.NPCType<NPCs.Bosses.SuperHardMode.Seath.SeathTheScalelessHead>());
            }
            else
            {
                Vector2 dustVel = UsefulFunctions.GenerateTargetingVector(NPC.Center, Main.npc[seathID.Value].Center, 24);
                dustVel = dustVel.RotatedByRandom(MathHelper.ToRadians(12));
                Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, 67, dustVel.X, dustVel.Y, 250, Color.White, 2f).noGravity = true;
                Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, 68, dustVel.X, dustVel.Y, 250, Color.White, 2f).noGravity = true;
                Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, 234, dustVel.X, dustVel.Y, 250, Color.White, 2f).noGravity = true;
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            return true;
        }

        public override void OnKill()
        {
            for (int i = 0; i < 50; i++)
            {
                int dust;
                Vector2 vel = Main.rand.NextVector2Circular(20, 20);
                dust = Dust.NewDust(NPC.Center, 30, 30, 234, vel.X, vel.Y, 240, default, 5f);
                Main.dust[dust].noGravity = true;
                dust = Dust.NewDust(NPC.Center, 30, 30, 226, vel.X, vel.Y, 200, default, 3f);
                Main.dust[dust].noGravity = true;
            }

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (Main.npc[i].type == ModContent.NPCType<SeathTheScalelessHead>() || Main.npc[i].type == ModContent.NPCType<SeathTheScalelessBody>() || Main.npc[i].type == ModContent.NPCType<SeathTheScalelessBody2>() || Main.npc[i].type == ModContent.NPCType<SeathTheScalelessBody3>() || Main.npc[i].type == ModContent.NPCType<SeathTheScalelessLegs>() || Main.npc[i].type == ModContent.NPCType<SeathTheScalelessTail>())
                {
                    if (Main.npc[i].active)
                    {
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