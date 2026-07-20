using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Weapons
{
    /// <summary>Visible ground marker that becomes a short-lived vertical fire pillar. ai[0] is
    /// the remaining warning time; ai[1] is 0 while warning and 1 while the pillar is damaging.</summary>
    public class PuppetFirefallPillar : ModProjectile
    {
        private bool Erupting => Projectile.ai[1] == 1f;

        public override string Texture => "Terraria/Images/MagicPixel";

        public override void SetDefaults()
        {
            Projectile.width = 48;
            Projectile.height = 4;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 180;
            Projectile.netImportant = true;
            Projectile.DamageType = DamageClass.Melee;
        }

        public override bool? CanDamage() => Erupting ? null : false;

        public override void AI()
        {
            Projectile.velocity = Vector2.Zero;

            if (!Erupting)
            {
                Projectile.ai[0]--;
                Lighting.AddLight(Projectile.Bottom, 0.45f, 0.14f, 0.02f);
                if (!Main.dedServ && Main.rand.NextBool(4))
                {
                    Dust warning = Dust.NewDustPerfect(
                        Projectile.Bottom + new Vector2(Main.rand.NextFloat(-22f, 22f), -2f),
                        DustID.Torch,
                        new Vector2(0f, Main.rand.NextFloat(-1.4f, -0.4f)),
                        120,
                        Color.OrangeRed,
                        Main.rand.NextFloat(0.65f, 0.95f));
                    warning.noGravity = true;
                }

                if (Projectile.ai[0] <= 0f)
                    BeginEruption();
                return;
            }

            Lighting.AddLight(Projectile.Center, 1.1f, 0.38f, 0.04f);
            if (!Main.dedServ)
            {
                for (int i = 0; i < 4; i++)
                {
                    Dust flame = Dust.NewDustPerfect(
                        Projectile.Bottom + new Vector2(
                            Main.rand.NextFloat(-Projectile.width * 0.4f, Projectile.width * 0.4f),
                            Main.rand.NextFloat(-Projectile.height, -4f)),
                        Main.rand.NextBool() ? DustID.Torch : DustID.GoldFlame,
                        new Vector2(Main.rand.NextFloat(-0.8f, 0.8f), Main.rand.NextFloat(-3.5f, -1.2f)),
                        60,
                        Color.OrangeRed,
                        Main.rand.NextFloat(1.0f, 1.65f));
                    flame.noGravity = true;
                }
            }
        }

        private void BeginEruption()
        {
            Vector2 bottom = Projectile.Bottom;
            Projectile.ai[1] = 1f;
            Projectile.width = 36;
            Projectile.height = 160;
            Projectile.Bottom = bottom;
            Projectile.timeLeft = 22;
            Projectile.netUpdate = true;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            if (!Erupting)
            {
                float pulse = 0.55f + 0.25f * (float)System.Math.Sin(Main.GlobalTimeWrappedHourly * 8f);
                Main.EntitySpriteDraw(
                    pixel,
                    Projectile.Bottom - Main.screenPosition,
                    null,
                    Color.OrangeRed * pulse,
                    0f,
                    new Vector2(0.5f),
                    new Vector2(48f, 3f),
                    SpriteEffects.None);
                for (int direction = -1; direction <= 1; direction += 2)
                {
                    Main.EntitySpriteDraw(
                        pixel,
                        Projectile.Bottom + new Vector2(direction * 20f, -5f) - Main.screenPosition,
                        null,
                        Color.Gold * pulse,
                        direction * 0.65f,
                        new Vector2(0.5f),
                        new Vector2(11f, 2f),
                        SpriteEffects.None);
                }
                return false;
            }

            float life = MathHelper.Clamp(Projectile.timeLeft / 8f, 0f, 1f);
            for (int i = 0; i < 9; i++)
            {
                float progress = i / 8f;
                float width = MathHelper.Lerp(30f, 8f, progress);
                Vector2 position = Projectile.Bottom
                    + new Vector2((i % 2 == 0 ? -1f : 1f) * (1f - progress) * 5f, -progress * Projectile.height)
                    - Main.screenPosition;
                Main.EntitySpriteDraw(
                    pixel,
                    position,
                    null,
                    Color.Lerp(Color.OrangeRed, Color.Gold, progress) * (0.82f * life),
                    0f,
                    new Vector2(0.5f),
                    new Vector2(width, 20f),
                    SpriteEffects.None);
            }
            return false;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.OnFire, 6 * 60);
        }
    }
}
