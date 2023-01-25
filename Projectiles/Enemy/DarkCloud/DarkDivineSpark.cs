using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.DarkCloud
{

    public class DarkDivineSpark : GenericLaser
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

            FollowHost = true;
            LaserOrigin = Main.npc[HostIdentifier].Center;
            FiringDuration = 120;
            TelegraphTime = 30;
            MaxCharge = 30;
            LaserLength = 5000;
            LaserColor = Color.Red;
            TileCollide = false;
            LaserSize = 1.3f;
            LaserTexture = TransparentTextureHandler.TransparentTextureType.RedLaserTransparent;
            LaserTextureHead = new Rectangle(0, 0, 30, 24);
            LaserTextureBody = new Rectangle(0, 26, 30, 30);
            LaserTextureTail = new Rectangle(0, 58, 30, 24);
            LaserSound = SoundID.Item12 with { Volume = 0.5f };

            LaserDebuffs = new List<int>(); 
            DebuffTimers = new List<int>();

            CastLight = false;

            LaserDebuffs.Add(BuffID.OnFire);
            DebuffTimers.Add(300);

            Additive = true;
        }



        Player targetPlayer;
        float rotDirection;

        bool rapid = false;
        public override void AI()
        {
            if (Math.Abs(Projectile.ai[0]) == 1)
            {
                SetBigLaserParameters();
                Projectile.velocity = Projectile.velocity.RotatedBy(0.05f * Projectile.ai[0]);
            }
            else
            {
                //Targeting mode
                SetTargetingLaserParameters();
            }
        }

        public void SetBigLaserParameters()
        {
            TelegraphTime = 0;
            LaserLength = 8000;
            LaserTexture = TransparentTextureHandler.TransparentTextureType.DarkDivineSpark;
            TileCollide = false;
            CastLight = true;
            LaserDust = 234;
            lightColor = Color.Indigo;
            MaxCharge = 0; //It fires instantly upon creation
            FiringDuration = 35;
            LaserSize = 3.5f;
            LaserTextureBody = new Rectangle(0, 24, 26, 30);
            LaserTextureHead = new Rectangle(0, 0, 26, 22);
            LaserTextureTail = new Rectangle(0, 56, 26, 22);
            LaserDust = 45;
            LineDust = true;
            frameCount = 15;
            LaserSound = null;
            LaserName = "Dark Divine Spark";
        }

        public void SetTargetingLaserParameters()
        {
            TelegraphTime = 99999;
            LaserLength = 8000;
            LaserColor = Color.Blue * 0.8f;
            TileCollide = false;
            CastLight = true;
            LaserDust = 234;
            MaxCharge = 0; //It will never fire, and is purely for telegraphing Dark Cloud's shot
            FiringDuration = (int)Projectile.ai[0]; //Set its duration to however long is left in this turn
            LaserSound = null;
            TargetingMode = 1;
        }
    }
}
