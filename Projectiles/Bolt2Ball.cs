using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles {
    class Bolt2Ball : ModProjectile {

        public override string Texture => "tsorcRevamp/Projectiles/Bolt1Ball";
        public override void SetDefaults() {
            projectile.width = 16;
            projectile.height = 16;
            projectile.penetrate = 1;
            projectile.friendly = true;
            projectile.tileCollide = true;
            projectile.magic = true;
            projectile.light = 0.3f;
        }
        public override void AI() {

            Lighting.AddLight(projectile.Center, .3f, .3f, .55f);
            projectile.rotation += projectile.velocity.X * 0.08f;

            Vector2 arg_2675_0 = new Vector2(projectile.position.X, projectile.position.Y);
            int arg_2675_1 = projectile.width;
            int arg_2675_2 = projectile.height;
            int arg_2675_3 = 15;
            float arg_2675_4 = 0f;
            float arg_2675_5 = 0f;
            int arg_2675_6 = 100;
            Color newColor = default(Color);
            if (Main.rand.Next(4) == 0)
            {
                int num47 = Dust.NewDust(arg_2675_0, arg_2675_1, arg_2675_2, arg_2675_3, arg_2675_4, arg_2675_5, arg_2675_6, newColor, 1.6f);
                Dust expr_2684 = Main.dust[num47];
                expr_2684.velocity *= 0.3f;

                Main.dust[num47].position.X = projectile.position.X + (float)(projectile.width / 2) + 4f + (float)Main.rand.Next(-4, 5);
                Main.dust[num47].position.Y = projectile.position.Y + (float)(projectile.height / 2) + (float)Main.rand.Next(-4, 5);
                Main.dust[num47].noGravity = true;
            }

            if (Main.rand.Next(2) == 0)
            {
                int n1337 = Dust.NewDust(arg_2675_0, arg_2675_1, arg_2675_2, 172, arg_2675_4, arg_2675_5, arg_2675_6, newColor, 1.6f);
                Main.dust[n1337].position.X = projectile.position.X + (float)(projectile.width / 2) + 4f + (float)Main.rand.Next(-4, 5);
                Main.dust[n1337].position.Y = projectile.position.Y + (float)(projectile.height / 2) + (float)Main.rand.Next(-4, 5);
                Main.dust[n1337].noGravity = true;
                Main.dust[n1337].velocity *= 0.6f;
            }

            if (Main.rand.Next(2) == 0)
            {
                int n1337 = Dust.NewDust(arg_2675_0, arg_2675_1, arg_2675_2, 226, arg_2675_4, arg_2675_5, arg_2675_6, newColor, .4f);
                Main.dust[n1337].position.X = projectile.position.X + (float)(projectile.width / 2) + 4f + (float)Main.rand.Next(-4, 5);
                Main.dust[n1337].position.Y = projectile.position.Y + (float)(projectile.height / 2) + (float)Main.rand.Next(-4, 5);
                Main.dust[n1337].noGravity = true;
                Main.dust[n1337].velocity *= 1f;
            }
        }

        public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
        {
            if (Main.rand.Next(4) == 0)
            {
                target.AddBuff(mod.BuffType("ElectrocutedBuff"), 180);
            }
        }

        public override void Kill(int timeLeft) {
            Projectile.NewProjectile(projectile.position.X + projectile.width / 2, projectile.position.Y + projectile.height / 2, projectile.velocity.X, projectile.velocity.Y, ModContent.ProjectileType<Bolt2Bolt>(), projectile.damage, 4f, projectile.owner);
            Main.PlaySound(SoundID.NPCHit53.WithPitchVariance(.3f).WithVolume(.8f), new Vector2(projectile.position.X, projectile.position.Y));

        }
    }

}
