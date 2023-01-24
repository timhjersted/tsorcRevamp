using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Okiku
{

    public class DarkLaser : GenericLaser
    {
        public override string Texture => "tsorcRevamp/Projectiles/Enemy/Okiku/PoisonSmog";

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.hide = true;
            Projectile.timeLeft = 999;

            LaserSize = 1.3f;
            LaserTexture = TransparentTextureHandler.TransparentTextureType.RedLaserTransparent;
            LaserTextureHead = new Rectangle(0, 0, 30, 24);
            LaserTextureBody = new Rectangle(0, 26, 30, 30);
            LaserTextureTail = new Rectangle(0, 58, 30, 24);
            LaserSound = SoundID.Item12 with { Volume = 0.5f };

            LaserDebuffs = new List<int>(); 
            DebuffTimers = new List<int>();

            CastLight = false;

            Additive = true;

            ProjectileSource = true;
            FollowHost = true;

            LaserName = "Dark Laser";
            TelegraphTime = 300;
            MaxCharge = 240;
            FiringDuration = 940;
            LaserLength = 10000;
            LaserColor = Color.Purple;
            TileCollide = false;
            CastLight = true;
        }

        public override void AI()
        {
            Projectile.rotation += 0.01f;
            Projectile.velocity = (Projectile.rotation + MathHelper.TwoPi * Projectile.ai[0] / 5f).ToRotationVector2();
            base.AI();
        }
    }
}
