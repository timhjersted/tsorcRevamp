using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Projectiles.Enemy
{
    /// <summary>
    /// Hostile fireball fired in a synchronized volley by an Elemental pack. Uses vanilla Fireball art,
    /// travels straight until hitting a tile or player, fades cleanly over its last 15 ticks on timeout,
    /// and bursts with layered fire + smoke dust on every ending.
    /// </summary>
    public class ElementalFireball : ModProjectile
    {
        const int LifetimeTicks = 180;
        const int FadeTicks = 15;

        public override string Texture => UsefulFunctions.VanillaTextureFilepath(ProjectileID.Fireball, "Projectile");

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 1;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = false;
            Projectile.timeLeft = LifetimeTicks;
        }

        public override bool? CanDamage()
        {
            return Projectile.timeLeft > FadeTicks;
        }

        public override void AI()
        {
            float spinDirection = 1f;
            if (Projectile.velocity.X < 0f)
            {
                spinDirection = -1f;
            }

            Projectile.rotation += 0.28f * spinDirection;

            if (Main.dedServ)
            {
                return;
            }

            float fade = MathHelper.Clamp(Projectile.timeLeft / (float)FadeTicks, 0f, 1f);
            Lighting.AddLight(Projectile.Center, 0.85f * fade, 0.42f * fade, 0.12f * fade);

            // Body flame trail + finer trailing ember sparks (max scale 1.2 * 1.35 = 1.62).
            Vector2 backward = -Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Dust flame = Dust.NewDustPerfect(
                Projectile.Center + Main.rand.NextVector2Circular(5f, 5f),
                DustID.Torch,
                backward * Main.rand.NextFloat(0.8f, 2.2f) + Main.rand.NextVector2Circular(0.6f, 0.6f),
                70,
                default,
                Main.rand.NextFloat(0.95f, 1.35f) * fade);
            flame.noGravity = true;

            if (Main.rand.NextBool(2))
            {
                Dust spark = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(6f, 6f),
                    DustID.OrangeTorch,
                    backward * Main.rand.NextFloat(1.4f, 3.2f),
                    50,
                    default,
                    Main.rand.NextFloat(0.55f, 0.9f) * fade);
                spark.noGravity = true;
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.OnFire, 4 * 60);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            float fade = MathHelper.Clamp(Projectile.timeLeft / (float)FadeTicks, 0f, 1f);
            Vector2 origin = texture.Size() * 0.5f;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;

            Main.EntitySpriteDraw(texture, drawPos, null, Color.White * fade, Projectile.rotation, origin, Projectile.scale * (0.4f + 0.6f * fade), SpriteEffects.None, 0f);
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
            {
                return;
            }

            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.55f, Pitch = 0.25f }, Projectile.Center);

            // Midground fire body (20 motes).
            for (int i = 0; i < 20; i++)
            {
                Vector2 direction = Main.rand.NextVector2Unit();
                Dust flame = Dust.NewDustPerfect(
                    Projectile.Center + direction * Main.rand.NextFloat(2f, 8f),
                    DustID.Torch,
                    direction * Main.rand.NextFloat(1.8f, 5.2f),
                    70,
                    default,
                    Main.rand.NextFloat(0.9f, 1.45f));
                flame.noGravity = true;
            }

            // Foreground fast sparks (10 motes).
            for (int i = 0; i < 10; i++)
            {
                Vector2 direction = Main.rand.NextVector2Unit();
                Dust spark = Dust.NewDustPerfect(
                    Projectile.Center,
                    DustID.OrangeTorch,
                    direction * Main.rand.NextFloat(4.5f, 8.5f),
                    50,
                    default,
                    Main.rand.NextFloat(0.5f, 0.85f));
                spark.noGravity = true;
            }

            // Background tinted smoke that billows and outlives the fire (8 motes).
            for (int i = 0; i < 8; i++)
            {
                Vector2 direction = Main.rand.NextVector2Unit();
                Dust smoke = Dust.NewDustPerfect(
                    Projectile.Center + direction * Main.rand.NextFloat(2f, 6f),
                    DustID.Smoke,
                    direction * Main.rand.NextFloat(0.6f, 2f) + new Vector2(0f, -0.8f),
                    150,
                    new Color(65, 48, 42),
                    Main.rand.NextFloat(0.6f, 0.85f));
                smoke.noGravity = true;
                smoke.noLight = true;
                smoke.fadeIn = Main.rand.NextFloat(1.3f, 1.65f);
            }
        }
    }
}
