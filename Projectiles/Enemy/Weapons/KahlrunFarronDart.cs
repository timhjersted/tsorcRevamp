using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Weapons
{
    /// <summary>Hostile counterpart to the player Farron Dart, including delayed volley telegraphs.</summary>
    public class KahlrunFarronDart : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/FarronDart";

        public override void SetDefaults()
        {
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = true;
            Projectile.timeLeft = 240;
            Projectile.penetrate = 1;
        }

        public override bool ShouldUpdatePosition() => Projectile.ai[0] <= 0f;

        public override bool CanHitPlayer(Player target) => Projectile.ai[0] <= 0f;

        public override void AI()
        {
            Projectile.rotation += 0.3f * (Projectile.direction == 0 ? 1 : Projectile.direction);

            if (Projectile.ai[0] > 0f)
            {
                Projectile.ai[0]--;
                Projectile.alpha = 90 + (int)(System.Math.Sin(Projectile.ai[0] * 0.7f) * 55f);
                Lighting.AddLight(Projectile.Center, 0.2f, 0.03f, 0.08f);
                return;
            }

            Projectile.alpha = 0;
            Lighting.AddLight(Projectile.Center, 0.2f, 0.2f, 0.35f);
            if (Main.rand.NextBool(2))
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 68, 0f, 0f, 100, default, 0.8f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= 0.1f;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Rectangle source = new Rectangle(0, 0, 6, 8);
            Color color = Color.White * (1f - Projectile.alpha / 255f);
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, source, color,
                Projectile.rotation, new Vector2(3f, 4f), Projectile.scale, SpriteEffects.None, 0);
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 10; i++)
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 68,
                    Projectile.velocity.X * 0.2f, Projectile.velocity.Y * 0.2f, 40, default, 0.9f);
                Main.dust[dust].noGravity = true;
            }

            SoundEngine.PlaySound(SoundID.NPCHit3 with { Volume = 0.25f }, Projectile.position);
        }
    }
}
