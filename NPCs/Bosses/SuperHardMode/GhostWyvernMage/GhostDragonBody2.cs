using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Bosses.SuperHardMode.GhostWyvernMage
{
    class GhostDragonBody2 : ModNPC
    {
        public override void SetDefaults()
        {
            npc.netAlways = true;
            npc.npcSlots = 1;
            npc.width = 45;
            npc.height = 45;
            drawOffsetY = GhostDragonHead.drawOffset;
            npc.aiStyle = 6;
            npc.knockBackResist = 0;
            npc.timeLeft = 750;
            npc.damage = 80;
            npc.defense = 20;
            npc.HitSound = SoundID.NPCHit7;
            npc.DeathSound = SoundID.NPCDeath8;
            npc.lifeMax = 35000;
            npc.noGravity = true;
            npc.noTileCollide = true;
            npc.value = 10000;
            npc.buffImmune[BuffID.Poisoned] = true;
            npc.buffImmune[BuffID.Confused] = true;
            npc.buffImmune[BuffID.OnFire] = true;
            npc.buffImmune[BuffID.CursedInferno] = true;
        }

        int fireDamage = 50;
        public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
        {
            npc.lifeMax = (int)(npc.lifeMax / 2);
            npc.damage = (int)(npc.damage / 2);
            fireDamage = (int)(fireDamage / 2);
        }
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Ghost Wyvern");
        }
        public override bool? DrawHealthBar(byte hbPosition, ref float scale, ref Vector2 position)
        {
            return false;
        }

        int Timer = -Main.rand.Next(800);

        public override bool CheckActive()
        {
            return false;
        }

        public override void AI()
        {

            npc.noTileCollide = true;
            npc.noGravity = true;
            npc.behindTiles = true;
            int[] bodyTypes = new int[] { ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonLegs>(), ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonLegs>(), ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonLegs>(), ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonLegs>(), ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonBody2>(), ModContent.NPCType<GhostDragonBody3>() };
            tsorcRevampGlobalNPC.AIWorm(npc, ModContent.NPCType<GhostDragonHead>(), bodyTypes, ModContent.NPCType<GhostDragonTail>(), 23, -2f, 15f, 0.23f, true, false);

            Timer++;

            if (!Main.npc[(int)npc.ai[1]].active)
            {
                npc.life = 0;

                npc.HitEffect(0, 10.0);
                npc.active = false;
            }
            //if (npc.position.X > Main.npc[(int)npc.ai[1]].position.X)
            //{
            //	npc.spriteDirection = 1;
            //}
            //if (npc.position.X < Main.npc[(int)npc.ai[1]].position.X)
            //{
            //	npc.spriteDirection = -1;
            //}

            if (Timer >= 0)
            {
                if (Main.netMode != 2)
                {
                    float num48 = 4f;
                    Vector2 vector8 = new Vector2(npc.position.X + (npc.width / 2), npc.position.Y + (npc.height / 2));
                    float rotation = (float)Math.Atan2(vector8.Y - (Main.player[npc.target].position.Y + (Main.player[npc.target].height * 0.5f)), vector8.X - (Main.player[npc.target].position.X + (Main.player[npc.target].width * 0.5f)));
                    rotation += Main.rand.Next(-50, 50) / 100;
                    int num54 = Projectile.NewProjectile(vector8.X, vector8.Y, (float)((Math.Cos(rotation) * num48) * -1), (float)((Math.Sin(rotation) * num48) * -1), ModContent.ProjectileType<Projectiles.Enemy.PoisonCrystalFire>(), fireDamage, 0f, 0);
                    Timer = -1200 - Main.rand.Next(1200);
                }
            }

            //if (Main.rand.Next(2)==0)
            //{
            //	int dust = Dust.NewDust(new Vector2((float) npc.position.X, (float) npc.position.Y), npc.width, npc.height, 62, 0, 0, //100, Color.White, 2.0f);
            //	Main.dust[dust].noGravity = true;
            //}


        }
    }
}