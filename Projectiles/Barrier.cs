
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles
{
    class Barrier : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Barrier");
        }
        public override void SetDefaults()
        {
            DrawHeldProjInFrontOfHeldItemAndArms = true; // Makes projectile appear in front of arms, not just in between body and arms
            Projectile.friendly = true;
            Projectile.width = 48;
            Projectile.height = 62;
            Projectile.penetrate = -1;
            Projectile.scale = 1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 2;
            Projectile.alpha = 160;
        }
        public override void AI()
        {
            if (Projectile.ai[0] == 0)
            {
                var player = Main.player[Projectile.owner];

                if (player.dead)
                {
                    Projectile.Kill();
                    return;
                }

                if (Main.rand.Next(3) == 0)
                {
                    int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 156, Projectile.velocity.X * 0f, Projectile.velocity.Y * 0f, 30, default(Color), .6f);
                    Main.dust[dust].noGravity = true;
                }


                Player projOwner = Main.player[Projectile.owner];
                projOwner.heldProj = Projectile.whoAmI; //this makes it appear in front of the player
                Projectile.velocity.X = player.velocity.X;
                Projectile.velocity.Y = player.velocity.Y;
                //projectile.position.X = player.position.X - (float)(player.width / 2);
                //projectile.position.Y = player.position.Y - (float)(player.height / 2);
            }
            //Barrier now has a second mode used exclusively by Attraidies, that occurs when its ai[0] is set to 1. It checks if he exists, and if not then dies.
            else
            {
                Projectile.timeLeft = 5;
                if (!NPC.AnyNPCs(ModContent.NPCType<NPCs.Special.AttraidiesApparition>()))
                {
                    Projectile.Kill();
                }
                else
                {
                    if (Main.rand.Next(3) == 0)
                    {
                        int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 156, Projectile.velocity.X * 0f, Projectile.velocity.Y * 0f, 30, default(Color), .6f);
                        Main.dust[dust].noGravity = true;
                    }
                }
            }
        }
        public override bool? CanDamage()
        {
            return false;
        }
    }
}