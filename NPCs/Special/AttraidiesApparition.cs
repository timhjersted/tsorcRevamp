using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Special
{
    class AttraidiesApparition : ModNPC //Attraidies that appears in the cavern of The Sorrow
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Attraidies");
            Main.npcFrameCount[NPC.type] = 2;
        }

        public override void SetDefaults()
        {
            NPC.npcSlots = 5;
            NPC.lifeMax = 80000;
            NPC.scale = 1f;
            NPC.defense = 10;
            NPC.height = 44;
            NPC.width = 28;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.dontTakeDamage = true;
            NPC.dontTakeDamageFromHostiles = true;
        }


        #region AI

        public override void AI()
        {
            Lighting.AddLight(NPC.Center, 1f, 0.75f, 1f);

            int dust1 = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 54, NPC.velocity.X, NPC.velocity.Y, 180, Color.Blue, 1f);
            Main.dust[dust1].noGravity = true;

            //"Movement"

            if (NPC.ai[0] == 0f)
            {
                if (NPC.velocity.Length() < 0.1)
                {
                    NPC.velocity.X = 0f;
                    NPC.velocity.Y = 0f;
                    NPC.ai[0] = 1f;
                    NPC.ai[1] = 45f;
                    return;
                }

                NPC.velocity *= 0.94f;
                if (NPC.velocity.X < 0f)
                {
                    NPC.direction = -1;
                }
                else
                {
                    NPC.direction = 1;
                }

                NPC.spriteDirection = NPC.direction;
                return;
            }

            if (Main.LocalPlayer.Center.X < NPC.Center.X)
            {
                NPC.direction = -1;
            }
            else
            {
                NPC.direction = 1;
            }

            NPC.spriteDirection = NPC.direction;
            NPC.ai[1] += 1f;
            float acceleration = 0.005f;
            if (NPC.ai[1] > 0f)
            {
                NPC.velocity.Y -= acceleration;
            }
            else
            {
                NPC.velocity.Y += acceleration;
            }

            if (NPC.ai[1] >= 90f)
            {
                NPC.ai[1] *= -1f;
            } // End Movement



            // Event duration & The Sorrow Spawning

            int lifetime = (int)NPC.ai[2];
            ++NPC.ai[2];

            if (lifetime == 1)
            {
                if (Main.netMode != NetmodeID.Server)
                {
                    Terraria.Audio.SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("tsorcRevamp/Sounds/Custom/EvilLaugh") with { Volume = 1.1f }, NPC.Center);
                }
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, new Vector2(0, 0), ModContent.ProjectileType<Projectiles.Barrier>(), 0, 0, Main.myPlayer, 1);
                }
            }

            if (lifetime > 100 && lifetime < 250)
            {
                Color color = new Color();
                int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 54, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, color, 4f);
                Main.dust[dust].noGravity = true;
            }
            if (lifetime > 250 && lifetime < 350)
            {
                Color color = new Color();
                int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 54, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, color, 4f);
                Main.dust[dust].noGravity = true;
                dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 29, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, color, 4f);
                Main.dust[dust].noGravity = true;
            }
            if (lifetime > 350)
            {
                Color color = new Color();
                int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 54, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, color, 4f);
                Main.dust[dust].noGravity = true;
                dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 29, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, color, 4f);
                Main.dust[dust].noGravity = true;
                dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 54, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, color, 4f);
                Main.dust[dust].noGravity = true;
                dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 29, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, color, 4f);
                Main.dust[dust].noGravity = true;
            }

            if (lifetime > 450)
            {
                for (int i = 0; i < 50; i++)
                {
                    Color color = new Color();
                    int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 54, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, color, 4f);
                    Main.dust[dust].noGravity = true;
                    dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 29, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, color, 4f);
                    Main.dust[dust].noGravity = true;
                    dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 54, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, color, 4f);
                    Main.dust[dust].noGravity = true;
                    dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 29, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, color, 4f);
                    Main.dust[dust].noGravity = true;
                    dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 29, Main.rand.Next(-50, 50) * 2, Main.rand.Next(-100, 100) * 2, 100, color, 4f);
                    Main.dust[dust].noGravity = true;
                }

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.position.X + (NPC.width / 2), (int)NPC.position.Y + (NPC.height / 2), ModContent.NPCType<NPCs.Bosses.TheSorrow>());
                }
                NPC.active = false;

            }
        }
        #endregion
    }
}