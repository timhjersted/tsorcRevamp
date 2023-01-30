using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Okiku
{
    public class StardustBeam : GenericLaser
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
            Projectile.timeLeft = 999;

            LaserSize = 1.1f;
            LaserTexture = TransparentTextureHandler.TransparentTextureType.RedLaserTransparent;
            LaserTextureHead = new Rectangle(0, 0, 30, 24);
            LaserTextureBody = new Rectangle(0, 26, 30, 30);
            LaserTextureTail = new Rectangle(0, 58, 30, 24);
            LaserSound = SoundID.Item12 with { Volume = 0.5f };

            LaserDebuffs = new List<int>();
            DebuffTimers = new List<int>();

            LaserName = "Stardust Beam";
            ProjectileSource = true;
            FollowHost = true;
            TelegraphTime = 300;
            FiringDuration = 120;
            LaserLength = 8000; //What could go wrong? Turns out, plenty!
            LaserColor = Color.DeepSkyBlue;
            TileCollide = false;
            CastLight = false;
            LaserDust = 234;
        }

        public override void AI()
        {
            MaxCharge = Projectile.ai[0];
            base.AI();
        }
    }
}
