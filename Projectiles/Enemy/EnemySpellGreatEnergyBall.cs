using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy;

class EnemySpellGreatEnergyBall : ModProjectile
{
    public override string Texture => "tsorcRevamp/Projectiles/Bolt1Ball";
    public override void SetDefaults()
    {
        Projectile.aiStyle = 1;
        Projectile.timeLeft = 100;
        Projectile.width = 16;
        Projectile.height = 16;
        Projectile.hostile = true;
        Projectile.penetrate = 1;
        Projectile.tileCollide = true;
        Projectile.ignoreWater = true;
    }
    public override void Kill(int timeLeft)
    {
        Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.Item94 with { Volume = 0.09f }, Projectile.Center); // electric thud quick
        //Terraria.Audio.SoundEngine.PlaySound(SoundID.Item, (int)projectile.position.X, (int)projectile.position.Y, 10);
        Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.position.X + (float)(Projectile.width / 2), Projectile.position.Y + (float)(Projectile.height - 16), 0, 0, ModContent.ProjectileType<EnemySpellGreatEnergyStrike>(), Projectile.damage, 3f, Projectile.owner);
        Vector2 projectilePos = new Vector2(Projectile.position.X - Projectile.velocity.X, Projectile.position.Y - Projectile.velocity.Y);
        int num41 = Dust.NewDust(projectilePos, Projectile.width, Projectile.height, 15, 0f, 0f, 100, default, 2f);
        Main.dust[num41].noGravity = true;
        Main.dust[num41].velocity *= 2f;
        Dust.NewDust(projectilePos, Projectile.width, Projectile.height, 15, 0f, 0f, 100, default, 1f);

    }

}
