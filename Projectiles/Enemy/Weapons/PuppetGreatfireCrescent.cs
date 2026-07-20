using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Weapons
{
    /// <summary>Pixel-drawn fire crescent used both as Owl Father's direct ranged slash and as the
    /// short ground waves released by Greatfire Breaker. ai[0]: 0 = direct, 1 = ground-following.</summary>
    public class PuppetGreatfireCrescent : ModProjectile
    {
        public const int DirectMode = 0;
        public const int GroundMode = 1;
        private const float GroundTravelLimit = 10f * 16f;

        private bool GroundFollowing => (int)Projectile.ai[0] == GroundMode;

        public override string Texture => "Terraria/Images/MagicPixel";

        public override void SetDefaults()
        {
            Projectile.width = 44;
            Projectile.height = 52;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = 1;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 54;
            Projectile.netImportant = true;
            Projectile.DamageType = DamageClass.Melee;
        }

        public override void AI()
        {
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.localAI[0] = 1f;
                if (GroundFollowing)
                {
                    Projectile.tileCollide = false;
                    Projectile.timeLeft = 38;
                }
            }

            if (GroundFollowing)
            {
                Projectile.localAI[1] += System.Math.Abs(Projectile.velocity.X);
                if (Projectile.localAI[1] >= GroundTravelLimit)
                {
                    Projectile.Kill();
                    return;
                }

                float groundY = PuppetGroundDustWave.FindGroundY(Projectile.Center.X, Projectile.Center.Y);
                Projectile.Center = new Vector2(Projectile.Center.X, groundY - Projectile.height * 0.5f);
                Projectile.velocity.Y = 0f;
            }
            else
            {
                Projectile.velocity *= 0.995f;
            }

            Projectile.rotation = Projectile.velocity.ToRotation();
            Lighting.AddLight(Projectile.Center, 0.9f, 0.35f, 0.06f);

            if (!Main.dedServ && Main.rand.NextBool(2))
            {
                Dust ember = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(18f, 22f),
                    Main.rand.NextBool() ? DustID.Torch : DustID.GoldFlame,
                    Projectile.velocity * 0.12f,
                    80,
                    Color.OrangeRed,
                    Main.rand.NextFloat(0.9f, 1.35f));
                ember.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            int direction = Projectile.velocity.X < 0f ? -1 : 1;
            float fade = MathHelper.Clamp(Projectile.timeLeft / 10f, 0f, 1f);

            for (int i = 0; i < 11; i++)
            {
                float progress = i / 10f;
                float angle = MathHelper.Lerp(-1.25f, 1.25f, progress);
                Vector2 local = new Vector2(
                    direction * (float)System.Math.Cos(angle) * 20f,
                    (float)System.Math.Sin(angle) * 25f);
                Vector2 drawPosition = Projectile.Center + local - Main.screenPosition;
                Color color = Color.Lerp(Color.Gold, Color.OrangeRed, System.Math.Abs(progress - 0.5f) * 1.4f)
                    * (0.85f * fade);
                float size = i == 0 || i == 10 ? 3f : 5f;
                Main.EntitySpriteDraw(
                    pixel,
                    drawPosition,
                    null,
                    color,
                    0f,
                    new Vector2(0.5f),
                    new Vector2(size, size + 1f),
                    SpriteEffects.None);
            }

            return false;
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Projectile.Kill();
            return false;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.OnFire, 6 * 60);
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < 9; i++)
            {
                Dust ember = Dust.NewDustPerfect(
                    Projectile.Center,
                    DustID.Torch,
                    Main.rand.NextVector2Circular(3.2f, 3.2f),
                    80,
                    Color.OrangeRed,
                    Main.rand.NextFloat(0.9f, 1.4f));
                ember.noGravity = true;
            }
        }
    }
}
