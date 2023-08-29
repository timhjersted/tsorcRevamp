using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy;

class EnemySpellLightning4Bolt : ModProjectile
{
    public override string Texture => "tsorcRevamp/Projectiles/Bolt4Bolt";
    public override void SetStaticDefaults()
    {
        Main.projFrames[Projectile.type] = 16;
    }

    public override void SetDefaults()
    {
        Projectile.penetrate = 8;
        Projectile.hostile = true;
        Projectile.tileCollide = false;
        Projectile.width = 130;
        Projectile.height = 402;
        DrawOffsetX = -55;
        DrawOriginOffsetY = -30;
    }
    public override void AI()
    {

        if (Projectile.ai[1] == 0f)
        {
            Terraria.Audio.SoundEngine.PlaySound(SoundID.DD2_EtherianPortalDryadTouch with { Volume = 0.5f }, Projectile.Center); //lightning sound
            Projectile.ai[1] = 1f;
            
        }
       

        

        Projectile.frameCounter++;
        Projectile.frame = (int)Math.Floor((double)Projectile.frameCounter / 4);

        if (Projectile.frame >= 16)
        {
            Projectile.frame = 15;
        }
        if (Projectile.frameCounter > 71)
        { // (projFrames * 4.5) - 1
            Projectile.alpha += 15;
        }

        if (Projectile.alpha >= 255)
        {
            Projectile.Kill();
        }
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Main.instance.LoadProjectile(Projectile.type);
        Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
        lightColor = Color.LightBlue;

        // Redraw the projectile with the color not influenced by light
        Vector2 drawOrigin = new Vector2(texture.Width * 0.5f, Projectile.height * 0.5f);
        for (int k = 0; k < Projectile.oldPos.Length; k++)
        {
            Vector2 drawPos = (Projectile.oldPos[k] - Main.screenPosition) + drawOrigin + new Vector2(0f, Projectile.gfxOffY);
            Color color = Projectile.GetAlpha(lightColor) * ((Projectile.oldPos.Length - k) / (float)Projectile.oldPos.Length);
            Main.EntitySpriteDraw(texture, drawPos, null, color, Projectile.rotation, drawOrigin, Projectile.scale, SpriteEffects.None, 0);
        }

        return true;
    }

}

