using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles
{
    class FireballNova : ModProjectile
    {

        public override void SetDefaults()
        {
            Projectile.width = 230;
            Projectile.height = 230;
            Projectile.scale = 1.1f;
            Projectile.timeLeft = 5;
            Projectile.hostile = false;
            Projectile.friendly = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 60;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.tileCollide = false;
            Projectile.penetrate = 999;
        }

        public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
        {
            target.AddBuff(BuffID.OnFire, 300);
        }

        float size = 130;
        float velocity = 14;
        int dustCount = 0;

        public override void AI()
        {
            if(Projectile.timeLeft == 5)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item62, Projectile.Center);
            }
            
            // create glowing red embers that fill the explosion's radius
            for (int i = 0; i < 120; i++)
            {
                Dust.NewDustPerfect(dustPos(), DustID.InfernoFork, dustVel(), 160, default, 2f).noGravity = true;
                Dust.NewDustPerfect(dustPos(), DustID.Torch, dustVel(), 160, default, 3f).noGravity = true;
                Dust.NewDustPerfect(dustPos(), DustID.InfernoFork, dustVel(), 160, default, 3f).noGravity = true;
                Dust.NewDustPerfect(Main.rand.NextVector2CircularEdge(15, 15), 130, Projectile.Center, 160, default, 3f).noGravity = true;
                Dust.NewDustPerfect(dustPos(), DustID.InfernoFork, dustVel(), 160, default, 3f).noGravity = true;
            }
        }
        Vector2 dustPos()
        {
            return Main.rand.NextVector2Circular(Projectile.width / 6, Projectile.height / 6) + Projectile.Center;
        }
        Vector2 dustVel()
        {
            return Main.rand.NextVector2Circular(30, 30);
        }
        public override void Kill(int timeLeft)
        {

        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (Vector2.DistanceSquared(projHitbox.Center.ToVector2(), targetHitbox.Center.ToVector2()) < 82944) //18 tile radius, 16 units per tile, (18*16)^2 = 82944
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






/*using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles
{
    class FireballInferno2 : ModProjectile
    {

        public override void SetDefaults()
        {
            
            
           
        }

        public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
        {
            target.AddBuff(BuffID.OnFire, 300);
        }

        
        public override void ModifyHitNPC(NPC target, ref int damage, ref float knockback, ref bool crit, ref int hitDirection)
        {
            Main.NewText("aaa");
            base.ModifyHitNPC(target, ref damage, ref knockback, ref crit, ref hitDirection);
        }
        
    }
}
*/