using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Projectiles.Enemy.Death
{
    /// <summary>
    /// Straight-line Reaper laser. It borrows EyeLaser's texture only and owns its motion so
    /// the configured speed is exact and never accelerates beyond it.
    /// </summary>
    class DeathReaperLaser : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.EyeLaser;

        public override void SetDefaults()
        {
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 180;
            Projectile.light = 1f;
            Projectile.DamageType = DamageClass.Generic;
            Projectile.netImportant = true;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Lighting.AddLight(Projectile.Center, new Vector3(1.1f, 0.35f, 1.6f));
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[ProjectileID.EyeLaser].Value;
            Vector2 origin = texture.Size() / 2f;
            Vector2 position = Projectile.Center - Main.screenPosition;
            Color glowColor = new Color(175, 70, 255);

            for (int i = 3; i >= 1; i--)
            {
                float scale = 1f + i * 0.32f;
                Main.EntitySpriteDraw(
                    texture,
                    position,
                    null,
                    glowColor * (0.10f * i),
                    Projectile.rotation,
                    origin,
                    scale,
                    SpriteEffects.None,
                    0f);
            }

            Main.EntitySpriteDraw(
                texture,
                position,
                null,
                lightColor,
                Projectile.rotation,
                origin,
                1f,
                SpriteEffects.None,
                0f);
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
            {
                return;
            }

            for (int i = 0; i < 8; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center,
                    DustID.PurpleTorch,
                    Main.rand.NextVector2Circular(2f, 2f),
                    80,
                    default,
                    Main.rand.NextFloat(0.8f, 1.1f));
                dust.noGravity = true;
            }
        }

    }
}
