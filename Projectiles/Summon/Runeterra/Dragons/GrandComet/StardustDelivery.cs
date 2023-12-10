using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Projectiles.Summon.Runeterra.Dragons.GrandComet
{
    public class StardustDelivery : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 8;
        }
        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 32;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.timeLeft = 900;
            Projectile.extraUpdates = 10;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
        }
        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            float projSpeed = 10f;

            Lighting.AddLight(Projectile.Center, Color.Navy.ToVector3() * 2f);
            Dust.NewDust(Projectile.Center, 2, 2, DustID.UltraBrightTorch, 0, 0, 150, default, 1.5f);

            Projectile.velocity = (player.Center - Projectile.Center).SafeNormalize(Vector2.Zero) * projSpeed;
            Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;
            if (Projectile.Hitbox.Intersects(player.Hitbox))
            {
                Projectile.Kill();
            }
        }
        public override void OnKill(int timeLeft)
        {
            Player player = Main.player[Projectile.owner];
            Rectangle PlayerRect = Utils.CenteredRectangle(player.Center, player.Size);
            player.GetModPlayer<tsorcRevampPlayer>().CenterOfTheUniverseStardustCount++;
            if (player.GetModPlayer<tsorcRevampPlayer>().CenterOfTheUniverseStardustCount == 10)
            {
                CombatText.NewText(PlayerRect, Color.Navy, LangUtils.GetTextValue("Items.CenterOfTheUniverse.MeteorReady"));
            }
            else
            {
                CombatText.NewText(player.Hitbox, Color.Navy, 1, true);
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
            return false;
        }
    }
}