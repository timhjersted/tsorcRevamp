using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Enemies {
    class ViciousSpit : ModNPC {
        public override void SetDefaults() {
            npc.width = 16;
            npc.height = 16;
            npc.aiStyle = -1;
            npc.damage = 165;
            npc.defense = 0;
            npc.lifeMax = 1;
            npc.HitSound = null;
            npc.noGravity = true;
            npc.noTileCollide = true;
            npc.DeathSound = SoundID.NPCDeath9;
            npc.alpha = 80;
            npc.timeLeft = 600;
        }

        public override void AI() {
            if (npc.target == 255) {
                npc.TargetClosest();
                //Vector2 shotOrigin = new Vector2(npc.position.X + (float)npc.width * 0.5f, npc.position.Y + (float)npc.height * 0.5f);
                //float distX = Main.player[npc.target].position.X + (float)(Main.player[npc.target].width / 2) - shotOrigin.X;
                //float distY = Main.player[npc.target].position.Y + (float)(Main.player[npc.target].height / 2) - shotOrigin.Y;
                //float distAbs = (float)Math.Sqrt(distX * distX + distY * distY);
                //distAbs = 7f / distAbs;
                //npc.velocity.X = distX * distAbs;
                //npc.velocity.Y = distY * distAbs;
            }
            npc.ai[0] += 1f;
            if (npc.ai[0] > 3f) {
                npc.ai[0] = 3f;
            }
            if (npc.ai[0] == 2f) {
                npc.position += npc.velocity;
                Main.PlaySound(SoundID.NPCKilled, (int)npc.position.X, (int)npc.position.Y, 9);
                for (int i = 0; i < 20; i++) {
                    int spawnDust = Dust.NewDust(new Vector2(npc.position.X, npc.position.Y + 2f), npc.width, npc.height, 26, 0f, 0f, 100, default, 1.8f);
                    Main.dust[spawnDust].velocity += npc.velocity;
                    Main.dust[spawnDust].noGravity = true;
                }
            }

            if (Collision.SolidCollision(npc.position, npc.width, npc.height)) {
                _ = Main.netMode;
                npc.StrikeNPCNoInteraction(999, 0f, 0);
            }
            for (int j = 0; j < 2; j++) {
                int trailDust = Dust.NewDust(new Vector2(npc.position.X, npc.position.Y + 2f), npc.width, npc.height, 26, npc.velocity.X * 0.1f, npc.velocity.Y * 0.1f, 80, default, 1.3f);
                Main.dust[trailDust].velocity *= 0.3f;
                Main.dust[trailDust].noGravity = true;
            }
        }
    }
}
