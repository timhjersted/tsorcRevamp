using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Magic;

class LightrifleFire : GenericLaser
{

    public override void SetStaticDefaults()
    {
        DisplayName.SetDefault("Lightrifle Fire");
    }

    public override string Texture => base.Texture;

    public override void SetDefaults()
    {
        Projectile.width = 10;
        Projectile.height = 10;
        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.tileCollide = true;

        FollowHost = false;
        TelegraphTime = 0;
        LaserColor = Color.Cyan;
        LaserDust = DustID.IceTorch;
        LineDust = true;
        LaserTexture = TransparentTextureHandler.TransparentTextureType.LightrifleFire;
        LaserTextureHead = new Rectangle(0, 0, 30, 24);
        LaserTextureBody = new Rectangle(0, 26, 30, 30);
        LaserTextureTail = new Rectangle(0, 58, 30, 24);
        LaserSize = 1f;
        DustAmount = 20;
        MaxCharge = 0;
        FiringDuration = 60;
        Projectile.penetrate = 999;

        Projectile.usesIDStaticNPCImmunity = true;
        Projectile.idStaticNPCHitCooldown = 6;
    }

    public override void AI()
    {
        if (Projectile.ai[1] > 0)
        {
            MaxCharge = Projectile.ai[1] * 5;
        }
        if(LaserOrigin == Vector2.Zero)
        {
            LaserOrigin = Projectile.Center;
        }
       // LaserColor *= 0.95f;
        base.AI();
    }
}
