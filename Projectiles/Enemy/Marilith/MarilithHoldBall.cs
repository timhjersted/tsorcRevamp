using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Marilith;

class MarilithHoldBall : ModProjectile
{

    public override void SetDefaults()
    {
        Projectile.hostile = true;
        Projectile.height = 16;
        Projectile.penetrate = 4;
        Projectile.tileCollide = true;
        Projectile.width = 16;
    }

    public override void AI()
    {
        Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
        if (Projectile.whoAmI % 2 == Main.GameUpdateCount % 2)
        {
            if (new Rectangle((int)Main.screenPosition.X - 100, (int)Main.screenPosition.Y - 100, Main.screenWidth + 100, Main.screenHeight + 100).Contains(Projectile.Center.ToPoint()))
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame, Vector2.Zero, 0, Color.Yellow, 2.0f);
                dust.noGravity = true;
                dust.noLight = true;
            }
        }
    }

    public override void OnHitPlayer(Player target, int damage, bool crit)
    {
        target.AddBuff(ModContent.BuffType<Buffs.MarilithHold>(), 30, false);
    }

    public override bool PreDraw(ref Color lightColor)
    {
        lightColor = Color.White;
        return base.PreDraw(ref lightColor);
    }
}
