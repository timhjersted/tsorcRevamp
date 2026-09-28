using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Projectiles.Enemy.Weapons
{
    public class ProximityMineExplosion : ModProjectile
    {
        // FireExplosion.png: five 52x52 frames. At 1.5 scale its visible footprint is 78x78.
        public const float DamageRadius = 39f;

        public static bool IsWithinDamageRadius(Vector2 center, Rectangle hitbox)
        {
            Vector2 nearest = new Vector2(
                MathHelper.Clamp(center.X, hitbox.Left, hitbox.Right),
                MathHelper.Clamp(center.Y, hitbox.Top, hitbox.Bottom));
            return Vector2.DistanceSquared(center, nearest) <= DamageRadius * DamageRadius;
        }

        public override string Texture => "tsorcRevamp/Content/Projectiles/Enemy/FireExplosion";
        public override void SetStaticDefaults() => Main.projFrames[Type] = 5;

        public override void SetDefaults()
        {
            Projectile.width = 78;
            Projectile.height = 78;
            Projectile.scale = 1.5f;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 20;
            Projectile.aiStyle = -1;
        }

        public override bool? CanDamage() => Projectile.timeLeft > 12 ? null : false;
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
            => IsWithinDamageRadius(Projectile.Center, targetHitbox);

        public override void OnSpawn(IEntitySource source)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.75f }, Projectile.Center);
            for (int i = 0; i < 75; i++)
            {
                int type = i < 15 ? DustID.Smoke : i < 45 ? DustID.OrangeTorch : DustID.Torch;
                Vector2 position = Projectile.Center + Main.rand.NextVector2Circular(24f, 24f);
                Dust dust = Dust.NewDustPerfect(position, type, Main.rand.NextVector2Circular(4f, 4f),
                    90, default, i < 15 ? 1f : 0.8f);
                dust.noGravity = true;
            }
        }

        public override void AI()
        {
            Projectile.frame = (20 - Projectile.timeLeft) / 4;
            if (!Main.dedServ)
                Lighting.AddLight(Projectile.Center, new Vector3(1f, 0.5f, 0.07f)
                    * (Projectile.timeLeft / 20f));
        }

        public override Color? GetAlpha(Color lightColor) => Color.White;
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
            => target.AddBuff(BuffID.OnFire, 6 * 60);
    }
}
