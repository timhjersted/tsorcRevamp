using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Content.Projectiles.Enemy.Basilisk
{
    /// <summary>
    /// Basilisk Hunter's plum curse breath (was the shared EnemyCursedBreath). A visible blob inside plum flame dust,
    /// both swelling 1x→1.2x over its 60-tick life. Passes through tiles. Every hit burns; some build powerful curse.
    /// </summary>
    class BasiliskHunterBreath : ModProjectile
    {
        public const int CursedFlamesTicks = 3 * 60;
        // 1 in N hits adds powerful curse (PowerfulCurseBuildup.ReApply adds 160 of the 500 meter).
        public const int PowerfulCurseChanceDenominator = 3;
        public const int PowerfulCurseMarkerTicks = 600 * 60;
        private const int LifetimeTicks = 60;

        // PLACEHOLDER sprite (copy of EnemyCursedBreath, still green) — replace with the plum retint.

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.aiStyle = 8;
            AIType = 96;
            Projectile.timeLeft = LifetimeTicks;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.penetrate = 3;
            Projectile.tileCollide = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.rotation += 3f;

            float lifeProgress = 1f - Projectile.timeLeft / (float)LifetimeTicks;
            float scale = MathHelper.Lerp(1f, 1.2f, lifeProgress);
            Projectile.scale = scale;
            Projectile.Resize((int)(16 * scale), (int)(16 * scale));

            if (Main.dedServ)
            {
                return;
            }

            // Pink flame multiplied by the plum meter colour; a fainter curse-powder wisp marks it as a curse.
            Dust flame = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.PinkTorch, 0, 0, 50, PowerfulCurseBuildup.PowerfulCurseColor, 3.0f * scale);
            flame.noGravity = true;

            if (Main.rand.NextBool(4))
            {
                Dust wisp = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.VilePowder, 0, 0, 150, PowerfulCurseBuildup.PowerfulCurseColor, 1.2f);
                wisp.noGravity = true;
            }

            Lighting.AddLight(Projectile.Center, PowerfulCurseBuildup.PowerfulCurseColor.ToVector3() * 0.8f);
        }

        public override bool PreKill(int timeLeft)
        {
            return false;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.CursedInferno, CursedFlamesTicks, false);

            if (Main.rand.NextBool(PowerfulCurseChanceDenominator))
            {
                target.AddBuff(ModContent.BuffType<PowerfulCurseBuildup>(), PowerfulCurseMarkerTicks, false);
            }
        }
    }
}
