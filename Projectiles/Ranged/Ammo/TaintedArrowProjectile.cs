using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Ranged.Ammo
{
    public class TaintedArrowProjectile : ModProjectile
    {
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = 5;
            Projectile.tileCollide = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.aiStyle = 1;
            Projectile.timeLeft = 600; 
            Projectile.damage = 30;
        }

        public override void AI()
        {
            Projectile.velocity.Y = Projectile.velocity.Y;

            Lighting.AddLight(Projectile.Center, 0.2f, 0.5f, 0.2f); 

            if (Main.rand.NextBool(5)) 
            {
                Dust dust = Dust.NewDustDirect(
                    Projectile.position,               
                    Projectile.width,                 
                    Projectile.height,                 
                    46,                               
                    Projectile.velocity.X * 0.2f,     
                    Projectile.velocity.Y * 0.2f,      
                    100,                               
                    default,                           
                    1f                              
                );

                dust.noGravity = false;                
                dust.velocity *= 0.5f;                
            }
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (tsorcRevamp.NatureNPCs.Contains(target.type))
            {
                modifiers.FinalDamage *= 1.20f;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Poisoned, 3 * 60);

            if (tsorcRevamp.NatureNPCs.Contains(target.type))
            {
                target.AddBuff(BuffID.ShadowFlame, 2 * 60);
            }
        }

        public override void OnKill(int timeLeft)
        {
            Collision.HitTiles(Projectile.position + Projectile.velocity, Projectile.velocity, Projectile.width, Projectile.height);
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Dig, Projectile.position);
        }

    }
}