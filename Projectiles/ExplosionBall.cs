using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles.VFX;

namespace tsorcRevamp.Projectiles
{
    class ExplosionBall : ModProjectile
    {

        public override string Texture => "tsorcRevamp/Projectiles/GreatFireballBall";

        public override void SetDefaults()
        {
            Projectile.friendly = true;
            Projectile.height = 16;
            Projectile.width = 16;
            Projectile.light = 0.8f;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 1;
            Projectile.tileCollide = true;
        }
        public override void AI()
        {

            int thisDust = Dust.NewDust(new Vector2(Projectile.position.X - Projectile.velocity.X, Projectile.position.Y - Projectile.velocity.Y), Projectile.width, Projectile.height, 6, 0, 0, 100, default, 2f);
            Main.dust[thisDust].noGravity = true;

            Projectile.rotation += 0.3f;
            int? closestNPC = UsefulFunctions.GetClosestEnemyNPC(Projectile.Center);
            if (closestNPC.HasValue)
            {
                UsefulFunctions.SmoothHoming(Projectile, Main.npc[closestNPC.Value].Center, 0.3f, 15f, Main.npc[closestNPC.Value].velocity, false);
            }
        }
        public override void OnKill(int timeLeft)
        {

            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item10, Projectile.Center);
            for (int i = 0; i < 20; i++)
            {
                int thisDust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 6, 0, 0, 100, default, 2f);
                Main.dust[thisDust].noGravity = true;
                thisDust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 6, 0, 0, 100, default, 2f); ;
                Main.dust[thisDust].noGravity = true;
            }

            Projectile.position -= Projectile.Size / 2;

            if (Main.myPlayer == Projectile.owner)
            {
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<ExplosionFlash>(), 0, 0, Main.myPlayer, 150, 20);

                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.position.X + (float)(Projectile.width), Projectile.position.Y + (float)(Projectile.height), 0, 0, ModContent.ProjectileType<Explosion>(), Projectile.damage, 8f, Projectile.owner);
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.position.X + (float)(Projectile.width * 4), Projectile.position.Y + (float)(Projectile.height), 0, 0, ModContent.ProjectileType<Explosion>(), Projectile.damage, 8f, Projectile.owner);
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.position.X + (float)(Projectile.width * -2), Projectile.position.Y + (float)(Projectile.height), 0, 0, ModContent.ProjectileType<Explosion>(), Projectile.damage, 8f, Projectile.owner);
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.position.X + (float)(Projectile.width), Projectile.position.Y + (float)(Projectile.height * -2), 0, 0, ModContent.ProjectileType<Explosion>(), Projectile.damage, 8f, Projectile.owner);
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.position.X + (float)(Projectile.width), Projectile.position.Y + (float)(Projectile.height * 4), 0, 0, ModContent.ProjectileType<Explosion>(), Projectile.damage, 8f, Projectile.owner);
            }


        }
    }
}
