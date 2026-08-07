using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{

    class Bubble : ModProjectile
    {

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.height = 20;
            Projectile.penetrate = 2;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.tileCollide = false;
            //AIType = 1;
            Projectile.width = 20;
            Projectile.timeLeft = 360;
        }

        //ai[1] == 1: the Hydromancer's upgraded barrage — soaks the target (Wet).
        bool Soaking => Projectile.ai[1] == 1f;

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            Projectile.tileCollide = false;
        }

        Vector2 initialVelocity;
        public override void AI()
        {
            if (Main.rand.NextBool(4))
            {
                Dust.NewDustPerfect(Projectile.Center, 29, null, 200, default, 1.4f).noGravity = true;
            }

            if (initialVelocity == Vector2.Zero)
            {
                initialVelocity = Projectile.velocity;
            }

            //Per-bubble phase offset so a volley bobs organically instead of in lockstep
            float rotation = Main.GameUpdateCount % (MathHelper.TwoPi * 10);
            rotation /= 10;
            rotation -= MathHelper.Pi;
            rotation += Projectile.whoAmI * 0.7f;

            Vector2 distortion;
            distortion.X = 0;
            distortion.Y = 5 * (float)Math.Sin(rotation);
            Projectile.velocity = initialVelocity + distortion;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            if (Soaking)
            {
                target.AddBuff(BuffID.Wet, 5 * 60); //soaked — ink sticks to a wet target
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            EnemyVFX.DrawQuaraBubble(Projectile.Center, Vector2.One * 24f, 0.5f, Soaking);
            return true;
        }

        public override bool PreKill(int timeLeft)
        {
            if (Soaking)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.SplashWeak with { Volume = 0.5f, Pitch = 0.6f }, Projectile.Center);
            }
            Projectile.type = ProjectileID.Bubble;
            return true;
        }
    }
}
