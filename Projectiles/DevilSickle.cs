using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Projectiles
{
    public class DevilSickle : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.width = 40;
            Projectile.height = 40;
            Projectile.scale = 0.9f;
            Projectile.alpha = 100;
            Projectile.timeLeft = 130;
            Projectile.friendly = true;
            Projectile.penetrate = 5;
            Projectile.tileCollide = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 30;
        }
        public override void AI()
        {
            int dust = Dust.NewDust(
            Projectile.position,
            Projectile.width,
            Projectile.height,
            6,
            Projectile.velocity.X,
            Projectile.velocity.Y,
            90,
            default,
            1.8f
            );
            Main.dust[dust].noGravity = true;
            Projectile.rotation += Projectile.direction * 0.8f;

            if (Projectile.velocity.X <= 7f && Projectile.velocity.Y <= 7f && Projectile.velocity.X >= -7f && Projectile.velocity.Y >= -7f)
            {
                Projectile.velocity.X *= 1.06f;
                Projectile.velocity.Y *= 1.06f;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.rand.NextBool(5))
            {
                target.AddBuff(BuffID.OnFire, 7 * 60); //420 blaze it 
            }
        }
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            if (Main.rand.NextBool(5) && info.PvP)
            {
                target.AddBuff(BuffID.OnFire, 7 * 60);
            }
        }
        public override void OnKill(int timeLeft)
        {
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Dig, Projectile.Center);
        }
    }
}
