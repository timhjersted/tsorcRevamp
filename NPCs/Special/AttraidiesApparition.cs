using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using System;
using tsorcRevamp.Projectiles.Enemy;

namespace tsorcRevamp.NPCs.Special
{
    class AttraidiesApparition : ModNPC //Attraidies that appears in the cavern of The Sorrow
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Attraidies");
            Main.npcFrameCount[npc.type] = 2;
        }

        public override void SetDefaults()
        {
            npc.npcSlots = 5;
            npc.lifeMax = 80000;
            npc.scale = 1f;
            npc.defense = 10;
            npc.height = 44;
            npc.width = 28;
            npc.noGravity = true;
            npc.noTileCollide = true;
            npc.dontTakeDamage = true;
            npc.dontTakeDamageFromHostiles = true;
        }


        #region AI

        public override void AI()
        {
            Lighting.AddLight(npc.Center, 1f, 0.75f, 1f);
            
            int dust1 = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 54, npc.velocity.X, npc.velocity.Y, 180, Color.Blue, 1f);
            Main.dust[dust1].noGravity = true;

            //"Movement"

            if (npc.ai[0] == 0f)
            {
                if (npc.velocity.Length() < 0.1)
                {
                    npc.velocity.X = 0f;
                    npc.velocity.Y = 0f;
                    npc.ai[0] = 1f;
                    npc.ai[1] = 45f;
                    return;
                }

                npc.velocity *= 0.94f;
                if (npc.velocity.X < 0f)
                {
                    npc.direction = -1;
                }
                else
                {
                    npc.direction = 1;
                }

                npc.spriteDirection = npc.direction;
                return;
            }

            if (Main.LocalPlayer.Center.X < npc.Center.X)
            {
                npc.direction = -1;
            }
            else
            {
                npc.direction = 1;
            }

            npc.spriteDirection = npc.direction;
            npc.ai[1] += 1f;
            float acceleration = 0.005f;
            if (npc.ai[1] > 0f)
            {
                npc.velocity.Y -= acceleration;
            }
            else
            {
                npc.velocity.Y += acceleration;
            }

            if (npc.ai[1] >= 90f)
            {
                npc.ai[1] *= -1f;
            } // End Movement



            // Event duration & The Sorrow Spawning

            int lifetime = (int)npc.ai[2];
            ++npc.ai[2];

            if (lifetime == 1)
            {
                if (Main.netMode != NetmodeID.Server)
                {
                    Main.PlaySound(mod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/EvilLaugh").WithVolume(1.1f), npc.Center);
                }
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(npc.Center, new Vector2(0, 0), ModContent.ProjectileType<Projectiles.Barrier>(), 0, 0, Main.myPlayer, 1);
                }
            }

            if (lifetime > 100 && lifetime < 250)
            {
                Color color = new Color();
                int dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 54, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, color, 4f);
                Main.dust[dust].noGravity = true;
            }
            if (lifetime > 250 && lifetime < 350)
            {
                Color color = new Color();
                int dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 54, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, color, 4f);
                Main.dust[dust].noGravity = true;
                dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 29, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, color, 4f);
                Main.dust[dust].noGravity = true;
            }
            if (lifetime > 350)
            {
                Color color = new Color();
                int dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 54, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, color, 4f);
                Main.dust[dust].noGravity = true;
                dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 29, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, color, 4f);
                Main.dust[dust].noGravity = true;
                dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 54, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, color, 4f);
                Main.dust[dust].noGravity = true;
                dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 29, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, color, 4f);
                Main.dust[dust].noGravity = true;
            }

            if (lifetime > 450)
            {
                for (int i = 0; i < 50; i++)
                {
                    Color color = new Color();
                    int dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 54, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, color, 4f);
                    Main.dust[dust].noGravity = true;
                    dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 29, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, color, 4f);
                    Main.dust[dust].noGravity = true;
                    dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 54, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, color, 4f);
                    Main.dust[dust].noGravity = true;
                    dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 29, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, color, 4f);
                    Main.dust[dust].noGravity = true;
                    dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 29, Main.rand.Next(-50, 50) * 2, Main.rand.Next(-100, 100) * 2, 100, color, 4f);
                    Main.dust[dust].noGravity = true;
                }

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    NPC.NewNPC((int)npc.position.X + (npc.width / 2), (int)npc.position.Y + (npc.height / 2), ModContent.NPCType<NPCs.Bosses.TheSorrow>());
                }
                npc.active = false;

            }
        }
        #endregion
    }
}