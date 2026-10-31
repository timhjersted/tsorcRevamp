using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Content.Projectiles.Enemy
{
    // Dust-only frost puffs. Intentional texture sharing with the original breath; the dragons
    // keep their existing projectile, lifetime and on-hit debuffs.
    public class BarrowWightBreath : ModProjectile
    {
        public const int ExposureBuildupPerTick = 1;
        private const int LifetimeTicks = 30;
        private static ulong _lastExposureTick = ulong.MaxValue;

        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(FrozenDragonsBreath));

        public override void SetDefaults()
        {
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.timeLeft = LifetimeTicks;
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
            Projectile.coldDamage = true;
            Projectile.DamageType = DamageClass.Magic;
        }

        public override void AI()
        {
            // Exposure is measured on the victim's machine, independently of damage/i-frames.
            // All Nemesis puffs share this cap; overlapping puffs cannot multiply Frost gain.
            if (Main.netMode != NetmodeID.Server && _lastExposureTick != Main.GameUpdateCount)
            {
                Player player = Main.LocalPlayer;
                if (player.active && !player.dead && Projectile.Hitbox.Intersects(player.Hitbox))
                {
                    _lastExposureTick = Main.GameUpdateCount;
                    FrostBuildup.Apply(player, ExposureBuildupPerTick);
                }
            }

            if (!Main.dedServ)
            {
                float fade = MathHelper.Clamp(Projectile.timeLeft / 10f, 0f, 1f);
                for (int i = 0; i < 2; i++)
                {
                    Dust frost = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height,
                        DustID.Frost, Projectile.velocity.X * 0.12f, Projectile.velocity.Y * 0.12f,
                        (int)MathHelper.Lerp(255f, 90f, fade), Color.LightBlue, 1.2f * fade);
                    frost.noGravity = true;
                    frost.fadeIn = 0f;
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
            {
                return;
            }

            // A stream has 45 puffs, so each puff dissipates with three motes rather than an explosion.
            for (int i = 0; i < 3; i++)
            {
                Dust frost = Dust.NewDustPerfect(Projectile.Center, DustID.Frost,
                    Main.rand.NextVector2Circular(0.8f, 0.8f), 190, Color.LightBlue, 0.6f);
                frost.noGravity = true;
            }
        }
    }
}
