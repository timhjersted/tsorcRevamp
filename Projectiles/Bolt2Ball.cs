using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles
{
    class Bolt2Ball : ModProjectile
    {

        public override string Texture => "tsorcRevamp/Projectiles/Bolt1Ball";
        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.penetrate = 1;
            Projectile.friendly = true;
            Projectile.tileCollide = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.light = 0.3f;
        }
        public override void AI()
        {

            if (Projectile.wet)
            {
                Projectile.timeLeft = 0;
            }

            Lighting.AddLight(Projectile.Center, .3f, .3f, .55f);
            Projectile.rotation += Projectile.velocity.X * 0.08f;

            Vector2 arg_2675_0 = new Vector2(Projectile.position.X, Projectile.position.Y);
            int arg_2675_1 = Projectile.width;
            int arg_2675_2 = Projectile.height;
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

                Main.dust[num47].position.X = Projectile.position.X + (float)(Projectile.width / 2) + 4f + (float)Main.rand.Next(-4, 5);
                Main.dust[num47].position.Y = Projectile.position.Y + (float)(Projectile.height / 2) + (float)Main.rand.Next(-4, 5);
                Main.dust[num47].noGravity = true;
            }

            if (Main.rand.Next(2) == 0)
            {
                int n1337 = Dust.NewDust(arg_2675_0, arg_2675_1, arg_2675_2, 172, arg_2675_4, arg_2675_5, arg_2675_6, newColor, 1.6f);
                Main.dust[n1337].position.X = Projectile.position.X + (float)(Projectile.width / 2) + 4f + (float)Main.rand.Next(-4, 5);
                Main.dust[n1337].position.Y = Projectile.position.Y + (float)(Projectile.height / 2) + (float)Main.rand.Next(-4, 5);
                Main.dust[n1337].noGravity = true;
                Main.dust[n1337].velocity *= 0.6f;
            }

            if (Main.rand.Next(2) == 0)
            {
                int n1337 = Dust.NewDust(arg_2675_0, arg_2675_1, arg_2675_2, 226, arg_2675_4, arg_2675_5, arg_2675_6, newColor, .4f);
                Main.dust[n1337].position.X = Projectile.position.X + (float)(Projectile.width / 2) + 4f + (float)Main.rand.Next(-4, 5);
                Main.dust[n1337].position.Y = Projectile.position.Y + (float)(Projectile.height / 2) + (float)Main.rand.Next(-4, 5);
                Main.dust[n1337].noGravity = true;
                Main.dust[n1337].velocity *= 1f;
            }
        }

        public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
        {
            if (Main.rand.Next(4) == 0)
            {
                target.AddBuff(Mod.Find<ModBuff>("ElectrocutedBuff").Type, 180);
            }
        }

        public override void Kill(int timeLeft)
        {
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.position.X + Projectile.width / 2, Projectile.position.Y + Projectile.height / 2, Projectile.velocity.X, Projectile.velocity.Y, ModContent.ProjectileType<Bolt2Bolt>(), Projectile.damage, 4f, Projectile.owner);
            Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit53 with { Volume = 0.8f, PitchVariance = 0.3f }, Projectile.Center);
        }
    }

}
