using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy;

class WaterTrail : ModProjectile
{
    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.TrailCacheLength[Projectile.type] = 5; // The length of old position to be recorded
        ProjectileID.Sets.TrailingMode[Projectile.type] = 0; // The recording mode
    }

    public override void SetDefaults()
    {
        Projectile.penetrate = 6;
        Projectile.width = 16;
        Projectile.height = 16;
        Projectile.ignoreWater = true;
        Projectile.tileCollide = true;
        Projectile.hostile = true;
        
    }
    public override void AI()
    {
        if (Projectile.ai[0] == 1)
        {
            Projectile.tileCollide = false;
        }
        Projectile.rotation += 4f;
        if (Main.rand.NextBool(4))
        {
            int dust = Dust.NewDust(new Vector2((float)Projectile.position.X, (float)Projectile.position.Y), Projectile.width, Projectile.height, 29, 0, 0, 50, Color.Blue, 2.0f);
            Main.dust[dust].noGravity = false;
            Main.dust[dust].color = Color.DeepSkyBlue;
            Main.dust[dust].shader = GameShaders.Armor.GetSecondaryShader((byte)GameShaders.Armor.GetShaderIdFromItemId(ItemID.LivingOceanDye), Main.LocalPlayer);
        }
        Lighting.AddLight((int)(Projectile.position.X / 16f), (int)(Projectile.position.Y / 16f), 0.4f, 0.1f, 0.1f);

        if (Projectile.velocity.X <= 4 && Projectile.velocity.Y <= 4 && Projectile.velocity.X >= -4 && Projectile.velocity.Y >= -4)
        {
            float accel = 2f + (Main.rand.Next(10, 30) * 0.5f);
            Projectile.velocity.X *= accel;
            Projectile.velocity.Y *= accel;
        }
    }
    public override bool OnTileCollide(Vector2 oldVelocity)
    { //allow the projectile to bounce
        Projectile.penetrate--;
        if (Projectile.penetrate == 0)
        {
            Projectile.Kill();
        }
        Collision.HitTiles(Projectile.position + Projectile.velocity, Projectile.velocity, Projectile.width, Projectile.height);
        Terraria.Audio.SoundEngine.PlaySound(SoundID.Drip, Projectile.position);
        if (Projectile.velocity.X != oldVelocity.X)
        {
            Projectile.velocity.X = -oldVelocity.X;
        }
        if (Projectile.velocity.Y != oldVelocity.Y)
        {
            Projectile.velocity.Y = -oldVelocity.Y;
        }
        return false;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Main.instance.LoadProjectile(Projectile.type);
        Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
        lightColor = Color.DeepSkyBlue;

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
