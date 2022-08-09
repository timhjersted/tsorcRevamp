using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Marilith
{

    public class MarilithTargetingLaser : EnemyGenericLaser
    {


        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Laser");
        }

        public override string Texture => base.Texture;

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

            FollowHost = true;
            LaserOrigin = Main.npc[HostIdentifier].Center;
            TelegraphTime = 60;
            FiringDuration = 0;
            MaxCharge = 60;
            LaserLength = 5000;
            LaserColor = Color.Red;
            TileCollide = true;
            LaserSize = 1.3f;
            LaserTexture = TransparentTextureHandler.TransparentTextureType.RedLaserTransparent;
            LaserTextureHead = new Rectangle(0, 0, 30, 24);
            LaserTextureBody = new Rectangle(0, 26, 30, 30);
            LaserTextureTail = new Rectangle(0, 58, 30, 24);
            LaserSound = SoundID.Item12 with { Volume = 0.5f };
            TargetingMode = 1;
            CastLight = true;


            Additive = true;
        }



        Vector2 target;
        Vector2 initialTarget;
        Vector2 initialPosition;
        Player targetPlayer;
        bool aimLeft = false;
        Vector2 simulatedVelocity;
        public override void AI()
        {
            if (Projectile.ai[0] == 1)
            {
                LaserColor = Color.Blue;
            }

            base.AI();
            if (Charge == MaxCharge - 1)
            {
                LaserSize = 0;
                LaserAlpha = 0;
            }

            if (LaserSize < 1.3f)
            {
                LaserSize += (1.3f / 30f);
                LaserAlpha += 1f / 30f;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {            
            return base.PreDraw(ref lightColor);
        }
    }
}
