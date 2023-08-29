using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.DarkCloud;


public class AntimatTargeting : GenericLaser
{
    public override string Texture => "tsorcRevamp/Projectiles/Enemy/Okiku/PoisonSmog";

    public override void SetDefaults()
    {
        base.SetDefaults();

        FollowHost = true;
        TelegraphTime = 285;
        MaxCharge = 0;
        LaserLength = 4000;
        LaserColor = Color.Red * 0.5f; //Forbidden secret color "half red"
        LightColor = Color.OrangeRed;
        TileCollide = false;
        CastLight = true;
        LaserSound = null;
        TargetingMode = 1;
        LaserSize = 0.6f;
    }

    Vector2 initialDirection = Vector2.Zero;
    float amplitude = 0.25f;
    public override void AI()
    {
        base.AI();
        if(Projectile.ai[0] > 3)
        {
            amplitude = 0.1f;
            TelegraphTime = (int)Projectile.ai[0];
            Projectile.ai[0] = 0;
        }

        if(TelegraphTime == 60)
        {
            TargetingMode = 2;
        }

        //Get a vector of length 128 pointing from dark cloud to the player, then rotate it by 90 degrees
        Vector2 offset = Main.player[Main.npc[HostIdentifier].target].Center - Main.npc[HostIdentifier].Center;
        offset.Normalize();
        //Multiply it by ((300 - AttackModeCounter) / 300), meaning as AttackModeCounter increases and approaches 0, the offset distance shrinks down
        //Then multiply it by Math.Sin, using AttackModeCounter as the parameter because it changes smoothly. Then add 120 * i, so each laser is offset by 120 degrees
        offset = offset.RotatedBy(amplitude * (TelegraphTime / 285f) * Math.Sin((Projectile.ai[0] * MathHelper.TwoPi / 3f) + Main.GameUpdateCount / 20f));
        //offset *= ((300f - Main.GameUpdateCount) / 300f) * (float)Math.Sin(MathHelper.ToRadians(2 * Main.GameUpdateCount + (120 * )));
        //offset *= ((300f - (Main.GameUpdateCount % 300f)) / 300f);
        //offset = offset.RotatedBy(MathHelper.ToRadians((Main.GameUpdateCount % 300f) + (120f * Projectile.ai[0])));
        Projectile.velocity = offset;
    }
}
