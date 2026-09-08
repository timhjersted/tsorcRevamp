using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Weapons
{
    /// <summary>
    /// A patch of burning ground left in the death goat's wake during its flame charge. Static, lasts
    /// three seconds, then burns out. Drawn from MagicPixel like PuppetFirefallPillar so it needs no
    /// sprite of its own — the flame is entirely dust plus a few stretched quads.
    /// </summary>
    public class DreadWraithEmberTrail : ModProjectile
    {
        public override string Texture => "Terraria/Images/MagicPixel";

        private const int LifetimeTicks = 3 * 60;
        private const int FireDebuffTicks = 4 * 60;

        public override void SetDefaults()
        {
            Projectile.width = 26;
            Projectile.height = 22;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = LifetimeTicks;
            Projectile.DamageType = DamageClass.Magic;
            // Repeat contact damage on a cooldown rather than once — this is a zone to be avoided, not a hit.
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 30;
        }

        public override void AI()
        {
            Projectile.velocity = Vector2.Zero;

            float lifeFraction = Projectile.timeLeft / (float)LifetimeTicks;

            Lighting.AddLight(Projectile.Center, 0.9f * lifeFraction, 0.30f * lifeFraction, 0.05f);

            if (Main.dedServ)
            {
                return;
            }

            // Flame density falls off as the patch burns out, so a dying patch visibly stops being a threat.
            int flameCount = Main.rand.NextBool() ? 3 : 2;

            for (int i = 0; i < flameCount; i++)
            {
                if (Main.rand.NextFloat() > lifeFraction + 0.25f)
                {
                    continue;
                }

                // Spawned a few px ABOVE the ground line and thrown hard upward. The previous version
                // drifted lazily and sat inside the tiles; this reads as fire climbing off the ground.
                Dust flame = Dust.NewDustPerfect(
                    Projectile.Bottom + new Vector2(Main.rand.NextFloat(-Projectile.width * 0.5f, Projectile.width * 0.5f), -6f),
                    Main.rand.NextBool(3) ? DustID.GoldFlame : DustID.Torch,
                    new Vector2(Main.rand.NextFloat(-0.5f, 0.5f), Main.rand.NextFloat(-6.5f, -3.2f)),
                    80,
                    default,
                    Main.rand.NextFloat(1.2f, 2f) * lifeFraction);
                flame.noGravity = true;
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.OnFire, FireDebuffTicks);
        }

        /// <summary>How far the flames climb before dissipating. 6 tiles, per design.</summary>
        private const float FlameRiseHeight = 6 * 16f;
        private const int RisingFlameCount = 4;

        public override bool PreDraw(ref Color lightColor)
        {
            // Deliberately NO ground-level slabs here. The previous version drew stretched quads anchored
            // at Bottom, which rendered as solid bars sinking into the tiles. Everything now travels UP.
            Texture2D flameTexture = ModContent.Request<Texture2D>("tsorcRevamp/Projectiles/Enemy/AbyssFlames").Value;
            Vector2 origin = flameTexture.Size() * 0.5f;
            float lifeFraction = Projectile.timeLeft / (float)LifetimeTicks;

            for (int i = 0; i < RisingFlameCount; i++)
            {
                // Each flame runs its own offset 0-1 climb cycle, so they stagger up the column instead of
                // rising in lockstep. Seeded off whoAmI so neighbouring patches don't sync up.
                float phase = (i / (float)RisingFlameCount) + Projectile.whoAmI * 0.13f;
                float climb = (Main.GlobalTimeWrappedHourly * 0.85f + phase) % 1f;

                float rise = climb * FlameRiseHeight;
                float fade = (1f - climb) * lifeFraction;   // dissipates as it reaches the top
                float scale = MathHelper.Lerp(1.15f, 0.35f, climb);

                // Slight horizontal weave so the column isn't a rigid vertical line.
                float drift = (float)System.Math.Sin(climb * MathHelper.TwoPi + i) * 5f;

                Main.EntitySpriteDraw(
                    flameTexture,
                    Projectile.Bottom + new Vector2(drift, -rise) - Main.screenPosition,
                    null,
                    Color.Lerp(Color.OrangeRed, Color.Gold, climb) * fade,
                    climb * MathHelper.TwoPi * 1.5f,   // spinning as it climbs
                    origin,
                    scale,
                    SpriteEffects.None,
                    0);
            }

            return false;
        }
    }
}
