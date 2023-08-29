using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.DarkCloud;


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

    bool initializedParameters = false;
    int direction = 0;
    public override void AI()
    {
        base.AI();
        if (Math.Abs(Projectile.ai[0]) == 999)
        {
            if (!initializedParameters)
            {
                SetBigLaserParameters();
                initializedParameters = true;
            }
            Projectile.velocity = Projectile.velocity.RotatedBy(-0.05f * direction);
        }
        else
        {
            //Targeting mode
            if (!initializedParameters)
            {
                SetTargetingLaserParameters();
                initializedParameters = true;
            }
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
        LightColor = Color.Indigo;
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
        LaserColor = Color.Cyan;
        LaserName = "Dark Divine Spark";

        if (Projectile.ai[0] > 0)
        {
            direction = 1;
        }
        else
        {
            direction = -1;
        }
    }

    public void SetTargetingLaserParameters()
    {
        TelegraphTime = (int)Projectile.ai[0];
        LaserLength = 8000;
        LaserColor = Color.Blue * 0.8f;
        TileCollide = false;
        CastLight = true;
        LaserDust = 234;
        MaxCharge = 0; //It will never fire, and is purely for telegraphing Dark Cloud's shot
        LaserSound = null;
        TargetingMode = 1;
    }
}
