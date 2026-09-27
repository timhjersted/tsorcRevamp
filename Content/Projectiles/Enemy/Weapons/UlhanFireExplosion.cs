using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Projectiles.Enemy.Weapons
{
    public class UlhanFireExplosion : ModProjectile
    {
        // Five 52x52 frames, four ticks each. First two frames hurt; the rest decay visibly.
        public override string Texture => "tsorcRevamp/Content/Projectiles/Enemy/FireExplosion";
        public override void SetStaticDefaults() => Main.projFrames[Type] = 5;

        public override void SetDefaults()
        {
            Projectile.width = 52;
            Projectile.height = 52;
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
            => Vector2.DistanceSquared(Projectile.Center, new Vector2(
                MathHelper.Clamp(Projectile.Center.X, targetHitbox.Left, targetHitbox.Right),
                MathHelper.Clamp(Projectile.Center.Y, targetHitbox.Top, targetHitbox.Bottom))) <= 26f * 26f;

        public override void OnSpawn(IEntitySource source)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.65f }, Projectile.Center);
            for (int i = 0; i < 70; i++)
            {
                int dustType = i < 12 ? DustID.Smoke : i < 45 ? DustID.OrangeTorch : DustID.Torch;
                Dust dust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(16f, 16f),
                    dustType, Main.rand.NextVector2Circular(3f, 3f), 90, default, i < 12 ? 0.9f : 0.65f);
                dust.noGravity = true;
            }
        }

        public override void AI()
        {
            Projectile.frame = (20 - Projectile.timeLeft) / 4;
            if (!Main.dedServ)
                Lighting.AddLight(Projectile.Center, new Vector3(0.8f, 0.3f, 0.05f) * (Projectile.timeLeft / 20f));
        }

        public override Color? GetAlpha(Color lightColor) => Color.White;
        public override void OnHitPlayer(Player target, Player.HurtInfo info) => target.AddBuff(BuffID.OnFire, 6 * 60);
    }
}
