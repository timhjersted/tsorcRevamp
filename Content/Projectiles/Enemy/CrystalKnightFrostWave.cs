using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Projectiles.Enemy
{
    /// <summary>
    /// One delayed, ground-anchored column of Permafrost Descent. Each side of the slam places
    /// fifteen columns one tile apart; their two-tick delays make the breath travel outward.
    /// The same rising height drives the frost dust and collision, then both fade together.
    /// </summary>
    public class CrystalKnightFrostWave : ModProjectile
    {
        private const int RiseTicks = 12;
        private const int HoldTicks = 9;
        private const int FadeTicks = 8;
        private const int ActiveTicks = RiseTicks + HoldTicks + FadeTicks;

        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(InvisibleNothingProj));

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 2;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = ActiveTicks;
        }

        public override void OnSpawn(IEntitySource source)
        {
            Projectile.timeLeft = ActiveTicks + (int)Projectile.ai[0];
        }

        private int Elapsed => ActiveTicks - Projectile.timeLeft;
        private float Height => Projectile.ai[1];

        public override bool? CanDamage() => Elapsed >= 2 && Elapsed < RiseTicks + HoldTicks;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (CanDamage() != true) return false;
            float rise = MathHelper.Clamp(Elapsed / (float)RiseTicks, 0f, 1f);
            int height = (int)(Height * rise);
            Rectangle breath = new Rectangle((int)Projectile.Center.X - 9,
                (int)Projectile.Center.Y - height, 18, height + 2);
            return breath.Intersects(targetHitbox);
        }

        public override void AI()
        {
            int elapsed = Elapsed;
            if (elapsed < 0 || Main.dedServ) return;
            float rise = MathHelper.Clamp(elapsed / (float)RiseTicks, 0f, 1f);
            float fade = MathHelper.Clamp((ActiveTicks - elapsed) / (float)FadeTicks, 0f, 1f);
            float visibleHeight = Height * rise;

            // CrystalSentryBreath's cyan Frost/IceTorch particles, stretched into a climbing fan.
            for (int i = 0; i < 5; i++)
            {
                float fraction = (i + Main.rand.NextFloat()) / 5f;
                Vector2 point = Projectile.Center + new Vector2(Main.rand.NextFloat(-8f, 8f),
                    -visibleHeight * fraction);
                Dust dust = Dust.NewDustPerfect(point, DustID.Frost,
                    new Vector2(Main.rand.NextFloat(-0.9f, 0.9f), Main.rand.NextFloat(-2.6f, -0.6f)),
                    110, Color.LightCyan, Main.rand.NextFloat(1.0f, 1.5f) * fade);
                dust.noGravity = true;
            }
            if (elapsed % 2 == 0)
                CrystalKnightShard.FrostDust(Projectile.Center - Vector2.UnitY * visibleHeight,
                    -Vector2.UnitY * 1.5f, 1.1f * fade);
            Lighting.AddLight(Projectile.Center - Vector2.UnitY * visibleHeight * 0.5f,
                new Vector3(0.08f, 0.2f, 0.28f) * fade);
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.Frostburn, 180);
            target.AddBuff(ModContent.BuffType<Buffs.Debuffs.TornWings>(), 540);
            Buffs.Debuffs.FrostBuildup.Apply(target);
        }

        public override void OnKill(int timeLeft)
        {
            if (Elapsed >= 0) CrystalKnightShard.Shatter(Projectile.Center, 5);
        }

        public override bool PreDraw(ref Color lightColor) => false;
    }
}
