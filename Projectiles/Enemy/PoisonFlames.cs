using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy;

class PoisonFlames : ModProjectile
{
    public override string Texture => "tsorcRevamp/Projectiles/Enemy/Okiku/PoisonSmog";
    public override void SetDefaults()
    {
        Projectile.width = 16;
        Projectile.height = 16;
        Projectile.alpha = 150;
        Projectile.aiStyle = 8;
        Projectile.timeLeft = 500;
        Projectile.damage = 46;
        Projectile.light = 0.8f;
        Projectile.penetrate = 1;
        Projectile.tileCollide = false;
        Projectile.hostile = true;
        AIType = 96; //pretendtype
    }
    public override bool PreAI()
    {
        Projectile.rotation += 1f;

        int dust = Dust.NewDust(new Vector2((float)Projectile.position.X, (float)Projectile.position.Y), Projectile.width, Projectile.height, 75, 0, 0, 50, Color.Chartreuse, 3.0f);
        Main.dust[dust].noGravity = true;

        if (Projectile.velocity.X <= 4 && Projectile.velocity.Y <= 4 && Projectile.velocity.X >= -4 && Projectile.velocity.Y >= -4)
        {
            float accel = 1f + (Main.rand.Next(10, 30) * 0.001f);
            Projectile.velocity.X *= accel;
            Projectile.velocity.Y *= accel;
        }
        return true;
    }
    public override void OnHitPlayer(Player target, int damage, bool crit)
    {
        target.AddBuff(BuffID.Poisoned, 2400);
        target.AddBuff(BuffID.Bleeding, 2400);
    }
}
