using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    /// <summary>Simple fast fireball fired in pairs from OwlCompanion's eyes after its 90-tick perch
    /// telegraph. Deliberately plain — the telegraph carries the read, not the shot.</summary>
    public class OwlEyeFlame : ModProjectile
    {
        // The comet-shaped sprite EnemyGreatFireAxeFireball used before it moved to MoltenOrb — free
        // to reuse here since nothing else claims it anymore.
        public override string Texture => "tsorcRevamp/Projectiles/Melee/Axes/AncientFireAxeFireball";

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 8; // native 66x28-per-frame strip
        }

        public override void SetDefaults()
        {
            // Scaled down from the sprite's native 66x28 — a smaller "eye flame" shot, not a full
            // overhead fireball.
            Projectile.width = 43;
            Projectile.height = 18;
            Projectile.scale = 0.65f;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 240;
            Projectile.light = 0.6f;
            Projectile.DamageType = DamageClass.Default;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            Lighting.AddLight(Projectile.Center, 0.7f, 0.3f, 0.05f);

            Projectile.frameCounter++;
            if (Projectile.frameCounter >= 5)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Type];
            }

            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.Torch,
                    Projectile.velocity * 0.1f, 100, default, Main.rand.NextFloat(1f, 1.5f));
                dust.noGravity = true;
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Projectile.Kill();
            return false;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.OnFire, 4 * 60);
        }

        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 8; i++)
            {
                Dust.NewDustPerfect(Projectile.Center, DustID.Torch,
                    Main.rand.NextVector2Circular(2.5f, 2.5f), 100, default,
                    Main.rand.NextFloat(0.9f, 1.4f)).noGravity = true;
            }
        }
    }
}
