using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Melee.Axes
{
    class AncientFireAxeFireball : ModProjectile
    {
        public int ProjectileLifetime = 600;
        public int TimeUntilOnHitBounce = 60;
        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 8;
        }
        public override void SetDefaults()
        {
            Projectile.width = 66;
            Projectile.height = 28;
            Projectile.friendly = true;
            Projectile.light = 0.8f;
            Projectile.alpha = 100;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = 6;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.timeLeft = ProjectileLifetime;
        }
        public override void OnSpawn(IEntitySource source)
        {
            Projectile.CritChance = (int)Projectile.ai[0];
            Projectile.ai[0] = 0;
        }
        public override void AI()
        {
            for (int num88 = 0; num88 < 2; num88++)
            {
                int num89 = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, 6, Projectile.velocity.X * 0.2f, Projectile.velocity.Y * 0.2f, 100, default(Color), 2f);
                Main.dust[num89].noGravity = true;
                Main.dust[num89].velocity.X *= 0.3f;
                Main.dust[num89].velocity.Y *= 0.3f;
            }
            Projectile.ai[1] += 1f;
            if (Projectile.ai[1] >= Math.Max(30, Main.rand.Next(61)))
            {
                Projectile.velocity.Y += 0.2f;
            }
            Projectile.rotation = Projectile.velocity.ToRotation();
            Lighting.AddLight(Projectile.Center, Projectile.light * 0.4f, Projectile.light * 0.1f, Projectile.light * 1f);
            Projectile.frameCounter++;
            int frameSpeed = 5;
            if (Projectile.frameCounter >= frameSpeed)
            {
                Projectile.frameCounter = 0;
                Projectile.frame++; 
                if (Projectile.frame >= Main.projFrames[Type])
                {
                    Projectile.frame = 0;
                }
            }
        }
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Bounce();
            return false;
        }
        public void Bounce()
        {
            if (Main.rand.NextBool(2))
            {
                Projectile.velocity = -Projectile.velocity;
                Projectile.velocity.Y *= 1f + Main.rand.NextFloat();
            }
            else
            {
                Projectile.velocity.Y = -Projectile.velocity.Y;
                Projectile.velocity.Y *= 1f + Main.rand.NextFloat();
            }
            Projectile.penetrate--;
            if (Projectile.owner == Main.myPlayer)
            {
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<AncientFireAxeFireballBurst>(), Projectile.damage / 4, Projectile.knockBack, Main.myPlayer, Projectile.CritChance);
            }
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Projectile.timeLeft < (ProjectileLifetime - TimeUntilOnHitBounce))
            {
                Bounce();
            }
            if (Main.rand.NextBool(2))
            {
                target.AddBuff(BuffID.OnFire, 5 * 60);
            }
        }
    }

}
