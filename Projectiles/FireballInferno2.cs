using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles
{
    class FireballInferno2 : ModProjectile
    {

        public override void SetDefaults()
        {
            Projectile.width = 230;
            Projectile.height = 230;
            Projectile.scale = 1.1f;
            Projectile.timeLeft = 300;
            Projectile.hostile = false;
            Projectile.friendly = false;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 5;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.tileCollide = false;
            Projectile.penetrate = 999;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire, 300);
        }

        float size = 130;
        float velocity = 14;
        int dustCount = 0;
        int originalDamage = 0;
        public override void AI()
        {
            if(Projectile.timeLeft > 295)
            {
                return;
            }
            if(originalDamage == 0)
            {
                originalDamage = Projectile.damage;
                Projectile.damage /= 50;
                Projectile.friendly = true;
            }
            if (size > 1)
            {
                size += velocity;
                velocity--;
                dustCount = (int)(2 * MathHelper.Pi * size / 10); //Spawn dust according to its size                
            }
            else
            {
                //Fade out after reaching max radius, and then despawn
                dustCount /= 2;
                if (dustCount <= 5)
                {
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<FireballNova>(), originalDamage, 0, default);
                    Projectile.Kill();
                    return;
                }
            }

            for (int j = 0; j < dustCount * 2; j++)
            {
                if (velocity > 0)
                {
                    Vector2 dir = Main.rand.NextVector2CircularEdge(size, size);
                    Vector2 dustPos = Projectile.Center + dir + Main.rand.NextVector2Circular(8, 8);
                    
                    if (!Collision.IsWorldPointSolid(dustPos))
                    {
                        dir.Normalize();
                        Vector2 dustVel = dir;
                        Dust.NewDustPerfect(dustPos, DustID.InfernoFork, dustVel, 200, default, 1f).noGravity = true;
                    }
                }
                else
                {
                    Vector2 dir = Main.rand.NextVector2CircularEdge(size, size);
                    Vector2 dustPos = Projectile.Center + dir;
                    
                    if (!Collision.IsWorldPointSolid(dustPos))
                    {
                        dir.Normalize();
                        Vector2 dustVel = -dir;
                        Dust.NewDustPerfect(dustPos, DustID.InfernoFork, dustVel, 200, default, 1f).noGravity = true;
                    }
                }
            }

        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.DefenseEffectiveness /= 2;
            if (!target.boss && !target.dontTakeDamage && !target.immortal && target.type != ModContent.NPCType<NPCs.Bosses.TestBoss>())
            {
                Vector2 diff = target.Center - Projectile.Center;
                diff.Normalize();

                 target.velocity = -diff * 5;
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (Vector2.DistanceSquared(projHitbox.Center.ToVector2(), targetHitbox.Center.ToVector2()) < size * size)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
            return false;
        }

    }
}