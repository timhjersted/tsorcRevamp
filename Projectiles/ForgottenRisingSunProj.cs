using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles
{
    class ForgottenRisingSunProj : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.width = 33;
            Projectile.height = 33;
            Projectile.aiStyle = 3;
            Projectile.timeLeft = 2400;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.ownerHitCheck = true;
            Projectile.penetrate = 6;
        }
        public override void AI()
        {
            Projectile.rotation += Math.Sign(Projectile.velocity.X) * MathHelper.ToRadians(10f);
            if (Projectile.timeLeft < 2340f)
            {
                Projectile.tileCollide = false;
                Projectile.velocity = (Projectile.velocity + Projectile.DirectionTo(Main.player[Projectile.owner].Center)) * 0.98f;
                if (Main.player[Projectile.owner].Hitbox.Intersects(Projectile.Hitbox))
                {
                    Projectile.Kill();
                }
            }
            int dust1 = Dust.NewDust(Projectile.Center - new Vector2(2, 13), 1, 1, DustID.FlameBurst, Projectile.velocity.X, Projectile.velocity.Y, 0, default, 1.2f);
            Main.dust[dust1].noGravity = true;
            Main.dust[dust1].velocity *= 0.2f;
            int dust2 = Dust.NewDust(Projectile.Center + new Vector2(2, 10), 1, 1, DustID.FlameBurst, Projectile.velocity.X, Projectile.velocity.Y, 0, default, 1.2f);
            Main.dust[dust2].noGravity = true;
            Main.dust[dust2].velocity *= 0.2f;
            Projectile.localAI[0] = 0;
            Lighting.AddLight(Projectile.Center, 0.9f, 0.5f, 0f);
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Dig, Projectile.Center);
            if (Projectile.velocity.X != oldVelocity.X)
            {
                Projectile.velocity.X = 0f - oldVelocity.X;
            }
            if (Projectile.velocity.Y != oldVelocity.Y)
            {
                Projectile.velocity.Y = 0f - oldVelocity.Y;
            }

            return false;
        }
        
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Daybreak, 4 * 60);
        }
    }
}