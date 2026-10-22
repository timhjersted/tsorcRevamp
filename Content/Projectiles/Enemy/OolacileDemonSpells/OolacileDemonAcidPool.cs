using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Projectiles.Enemy.OolacileDemonSpells
{
    // Lingering acid left by a Bile Mortar glob: an 88x14 px strip sitting on the floor for LifetimeTicks (3s).
    // Harmless for its first ArmTicks while it spreads, then burns anyone standing in it (40t player i-frames ≈ 1 hit per 0.67s).
    // Dust-only. Thins out over the last FadeTicks. Spawned by the server, centred on the floor surface by the glob.
    public class OolacileDemonAcidPool : ModProjectile
    {
        public const int PoolWidth = 88;
        public const int PoolHeight = 14;
        private const int LifetimeTicks = 180;
        private const int ArmTicks = 12;
        private const int FadeTicks = 30;

        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(InvisibleNothingProj));

        public override void SetDefaults()
        {
            Projectile.width = PoolWidth;
            Projectile.height = PoolHeight;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = LifetimeTicks;
            Projectile.DamageType = DamageClass.Magic;
        }

        public override bool? CanDamage()
        {
            int age = LifetimeTicks - Projectile.timeLeft;
            if (age < ArmTicks)
            {
                return false;
            }

            return null;
        }

        public override void AI()
        {
            Projectile.velocity = Vector2.Zero;

            int age = LifetimeTicks - Projectile.timeLeft;
            float spread = MathHelper.Clamp(age / (float)ArmTicks, 0f, 1f);
            float fade = MathHelper.Clamp(Projectile.timeLeft / (float)FadeTicks, 0f, 1f);
            float strength = spread * fade;

            Lighting.AddLight(Projectile.Center, 0.1f * strength, 0.35f * strength, 0.05f * strength);

            if (Main.dedServ)
            {
                return;
            }

            // Surface: low green flames along the strip, widening from the middle as it spreads.
            float halfWidth = PoolWidth * 0.5f * spread;
            for (int i = 0; i < 2; i++)
            {
                Vector2 spot = new Vector2(Projectile.Center.X + Main.rand.NextFloat(-halfWidth, halfWidth), Projectile.Bottom.Y - 4f);
                Dust surface = Dust.NewDustPerfect(spot, DustID.CursedTorch, new Vector2(0f, -Main.rand.NextFloat(0.2f, 0.9f)), 60, default, 1.5f * strength + 0.3f);
                surface.noGravity = true;
            }

            // Bubbles popping up out of it every few ticks.
            if (Main.rand.NextBool(5))
            {
                Vector2 spot = new Vector2(Projectile.Center.X + Main.rand.NextFloat(-halfWidth, halfWidth), Projectile.Bottom.Y - 2f);
                Dust bubble = Dust.NewDustPerfect(spot, DustID.GreenTorch, new Vector2(0f, -Main.rand.NextFloat(1f, 2.2f)), 0, default, 1.1f * strength);
                bubble.noGravity = true;
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.Venom, 120, false);
        }
    }
}
