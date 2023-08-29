using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Graphics;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles.VFX;

namespace tsorcRevamp.Projectiles.Ranged;

class PiercingPlasma : DynamicTrail
{
    
    public override void SetStaticDefaults()
    {
        DisplayName.SetDefault("PiecingPlasma");
    }
    public override string Texture => "tsorcRevamp/Projectiles/Enemy/Triad/HomingStarStar";
    public override void SetDefaults()
    {
        Projectile.width = 20;
        Projectile.height = 20;
        Projectile.timeLeft = 600;
        Projectile.hostile = false;
        Projectile.friendly = true;
        Projectile.tileCollide = false;
        Projectile.penetrate = -1;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 10;

        trailWidth = 25;
        trailPointLimit = 150;
        trailYOffset = 30;
        trailMaxLength = 150;
        NPCSource = false;
        collisionPadding = 0;
        collisionEndPadding = 1;
        collisionFrequency = 2;
        customEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/DeathLaser", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
    }

    public override void OnHitPlayer(Player target, int damage, bool crit)
    {
        target.AddBuff(BuffID.OnFire, 100);
    }
    public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
    {
        target.AddBuff(BuffID.OnFire, 100);
    }

    bool playedSound = false;
    public override void AI()
    {
        base.AI();
        Lighting.AddLight(Projectile.Center, Color.Red.ToVector3());
        if (!playedSound)
        {
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item33 with { Volume = 0.5f}, Projectile.Center);
            playedSound = true;
        }
    }
    
    public override float CollisionWidthFunction(float progress)
    {
        return 9;
    }

    float timeFactor = 0;
    public override void SetEffectParameters(Effect effect)
    {
        collisionEndPadding = trailPositions.Count / 3;
        collisionPadding = trailPositions.Count / 8;
        visualizeTrail = false;
        timeFactor++;
        effect.Parameters["noiseTexture"].SetValue(tsorcRevamp.tNoiseTexture1);
        effect.Parameters["fadeOut"].SetValue(fadeOut);
        effect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly);

        Color shaderColor = new Color(1.0f, 0.4f, 0.4f, 1.0f);
        shaderColor = UsefulFunctions.ShiftColor(shaderColor, timeFactor, 0.03f);
        effect.Parameters["shaderColor"].SetValue(shaderColor.ToVector4());
        effect.Parameters["WorldViewProjection"].SetValue(GetWorldViewProjectionMatrix());
    }
}
