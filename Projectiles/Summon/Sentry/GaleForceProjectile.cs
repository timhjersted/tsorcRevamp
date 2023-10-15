using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Summon.Sentry
{
    public class GaleForceProjectile : ModProjectile
    {

        public override void SetStaticDefaults()
        {

            //Main.projFrames[Projectile.type] = 7;

            ProjectileID.Sets.IsADD2Turret[Projectile.type] = false;
            ProjectileID.Sets.TurretFeature[Projectile.type] = true;
            ProjectileID.Sets.MinionSacrificable[Projectile.type] = true;
        }

        public sealed override void SetDefaults()
        {
            Projectile.width = 104;
            Projectile.height = 93;
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 36000;

            Projectile.sentry = true;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
        }

        public override void AI()
        {

            Player owner = Main.player[Projectile.owner];


            if (Main.GameUpdateCount % 180 == 0)
            {
                Projectile.NewProjectile(Projectile.GetSource_None(), Projectile.Center, Main.MouseWorld - Projectile.Center, ProjectileID.FrostBlastFriendly, Projectile.damage, 1f, Main.myPlayer);
            }
        }

        private void Visuals()
        {
            // So it will lean slightly towards the direction it's moving
            Projectile.rotation = Projectile.velocity.X * 0.1f;

            if (Projectile.velocity.X > 0.05)
            {
                Projectile.spriteDirection = -1;
            }
            if (Projectile.velocity.X < -0.05)
            {
                Projectile.spriteDirection = 1;
            }

            // This is a simple "loop through all frames from top to bottom" animation
            int frameSpeed = 5;

            Projectile.frameCounter++;

            if (Projectile.frameCounter >= frameSpeed)
            {
                Projectile.frameCounter = 0;
                Projectile.frame++;

                if (Projectile.frame >= Main.projFrames[Projectile.type])
                {
                    Projectile.frame = 0;
                }
            }

            // Some visuals here
            Lighting.AddLight(Projectile.Center, Color.Gold.ToVector3() * 0.78f);
        }
    }
}