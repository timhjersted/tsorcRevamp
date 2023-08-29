using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.DarkCloud;

class DarkWave : ModProjectile
{
    public override string Texture => "tsorcRevamp/Projectiles/Enemy/DarkCloud/DarkCloudSpark";
    public override void SetDefaults()
    {
        Projectile.aiStyle = 0;
        Projectile.width = 26;
        Projectile.height = 26;
        Projectile.hostile = true;
        Projectile.penetrate = 20;
        Projectile.tileCollide = false;
        Projectile.timeLeft = 240;
    }


    public override void AI()
    {
        //Aka lmao if you think you can just outrun this
        Projectile.width = 10 + (240 - Projectile.timeLeft) / 2;
        Projectile.height = 10 + (240 - Projectile.timeLeft) / 2;
    }

    public override bool PreDraw(ref Color lightColor)
    {

        Vector2 offset = Main.rand.NextVector2CircularEdge(Projectile.width, Projectile.height);
        Vector2 velocity = new Vector2(-2, 0).RotatedBy(offset.ToRotation()) * Main.rand.NextFloat(2);
        Dust.NewDustPerfect(Projectile.Center + offset, DustID.ShadowbeamStaff, velocity, Scale: 3.5f).noGravity = true;

        return false;
    }
}