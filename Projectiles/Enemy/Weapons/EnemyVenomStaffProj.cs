using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Weapons
{
    /// <summary>
    /// Hostile purple venom orb fired in a shotgun spread by an invader's enemy Venom Staff.  Straight
    /// flight, applies the Venom debuff on hit.  Uses its own 4-frame animated sprite with a purple glow.
    /// </summary>
    public class EnemyVenomStaffProj : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/Enemy/Weapons/EnemyVenomStaffProj";

        private const int TotalFrames = 4;
        private const int TicksPerFrame = 5;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = TotalFrames;
        }

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 28;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.tileCollide = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 120;
            Projectile.ignoreWater = true;
            Projectile.light = 0.4f;
            Projectile.DamageType = DamageClass.Magic;
        }

        public override void AI()
        {
            Projectile.frameCounter++;
            if (Projectile.frameCounter >= TicksPerFrame)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % TotalFrames;
            }

            if (Main.rand.NextBool())
            {
                int d = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.VenomStaff,
                    Projectile.velocity.X * 0.2f, Projectile.velocity.Y * 0.2f, 100, default, 1.1f);
                Main.dust[d].noGravity = true;
            }
            Lighting.AddLight(Projectile.Center, 0.35f, 0.05f, 0.5f); // purple glow
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // Force bright purple tint so the orb reads as venom-purple regardless of ambient light.
            lightColor = Color.Lerp(lightColor, new Color(180, 50, 255), 0.75f);
            return true;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.Venom, 600); // 10 s venom
        }

        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 8; i++)
            {
                int d = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.VenomStaff,
                    Main.rand.NextFloat(-2.5f, 2.5f), Main.rand.NextFloat(-2.5f, 2.5f), 100, default, 1.4f);
                Main.dust[d].noGravity = true;
            }
        }
    }
}
