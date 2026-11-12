using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Content.Projectiles.Enemy
{
    /// <summary>
    /// The Bone Chaplain's Blight Flame: one short-range gout of black Blight fire from its palm. Straight, no gravity or
    /// homing, stops at walls. Each hit adds BlightPerHit Blight buildup (double a normal hit). Dust-only body: black
    /// Wraith smoke round a yellow core. 40t life with the last 8t harmless, so the damaging reach is ~224px at 7px/t.
    /// </summary>
    public class BoneChaplainBlightFlame : ModProjectile
    {
        public const int BlightPerHit = 30;
        const int LifetimeTicks = 40;
        const int HarmlessFadeTicks = 8;

        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(InvisibleNothingProj));

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = LifetimeTicks;
            Projectile.aiStyle = -1;
        }

        public override bool CanHitPlayer(Player target)
        {
            return Projectile.timeLeft > HarmlessFadeTicks;
        }

        public override void AI()
        {
            if (Main.dedServ)
            {
                return;
            }

            // The flame: dense black smoke, a yellow core, a few embers shed backwards. Thins out in the harmless fade.
            bool fading = Projectile.timeLeft <= HarmlessFadeTicks;
            int smokeCount = 3;
            if (fading)
            {
                smokeCount = 1;
            }

            for (int i = 0; i < smokeCount; i++)
            {
                Vector2 spot = Projectile.Center + Main.rand.NextVector2Circular(8f, 8f);
                Dust smoke = Dust.NewDustPerfect(spot, DustID.Wraith, Projectile.velocity * 0.2f, 60, default, 1.3f);
                smoke.noGravity = true;
            }

            if (!fading)
            {
                Dust core = Dust.NewDustPerfect(Projectile.Center, DustID.YellowTorch, Projectile.velocity * 0.4f, 40, default, 1.4f);
                core.noGravity = true;
            }

            if (Main.rand.NextBool(3))
            {
                Dust ember = Dust.NewDustPerfect(Projectile.Center, DustID.YellowTorch, -Projectile.velocity * 0.15f + Main.rand.NextVector2Circular(1f, 1f), 80, default, 0.8f);
                ember.noGravity = true;
            }

            Lighting.AddLight(Projectile.Center, 0.45f, 0.38f, 0.05f);
        }

        // Runs on the hit player's machine (hostile projectile): the buildup is that player's own state.
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            BlightBuildup.Apply(target, BlightPerHit);
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
            {
                return;
            }

            // Every ending (wall, timeout) gutters out in a small black-and-yellow burst - never a silent vanish.
            SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.35f, Pitch = -0.6f }, Projectile.Center);
            for (int i = 0; i < 16; i++)
            {
                int dustType = DustID.Wraith;
                if (i % 3 == 0)
                {
                    dustType = DustID.YellowTorch;
                }

                Vector2 velocity = Main.rand.NextVector2Circular(2.5f, 2.5f);
                Dust burst = Dust.NewDustPerfect(Projectile.Center, dustType, velocity, 60, default, 1.1f);
                burst.noGravity = true;
            }
        }
    }
}
