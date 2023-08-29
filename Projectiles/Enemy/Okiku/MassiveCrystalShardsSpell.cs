using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Okiku;

class MassiveCrystalShardsSpell : ModProjectile
{
    public override string Texture => "tsorcRevamp/Projectiles/Ice1Ball";
    public override void SetDefaults()
    {
        Projectile.aiStyle = 0;
        Projectile.hostile = true;
        Projectile.height = 16;
        Projectile.penetrate = 1;
        Projectile.scale = 1;
        Projectile.tileCollide = false;
        Projectile.width = 16;

    }

    public override void PostAI()
    {
        Lighting.AddLight(Projectile.position, Color.Cyan.ToVector3());
        Projectile.alpha += 5;
        if (Projectile.alpha >= 255)
        {
            Projectile.Kill();
        }
    }

    public override void Kill(int timeLeft)
    {

        //Terraria.Audio.SoundEngine.PlaySound(SoundID.Item, (int)projectile.position.X, (int)projectile.position.Y, 14);
        Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.Item30 with { Volume = 0.5f, Pitch = 0.1f }, Projectile.Center); //ice materialize - good
        var Shards = ModContent.ProjectileType<MassiveCrystalShards>();
        Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.position.X + (float)(Projectile.width), Projectile.position.Y + (float)(Projectile.height), 0, 5, Shards, (int)(this.Projectile.damage), 3f, Projectile.owner);
        Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.position.X + (float)(Projectile.width * 5), Projectile.position.Y + (float)(Projectile.height * 4), 0, 5, Shards, (int)(this.Projectile.damage), 3f, Projectile.owner);
        Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.position.X + (float)(Projectile.width * -3), Projectile.position.Y + (float)(Projectile.height * 7), 0, 5, Shards, (int)(this.Projectile.damage), 3f, Projectile.owner);
        Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.position.X + (float)(Projectile.width), Projectile.position.Y + (float)(Projectile.height * 10), 0, 5, Shards, (int)(this.Projectile.damage), 3f, Projectile.owner);
        Vector2 projectilePos = new Vector2(Projectile.position.X - Projectile.velocity.X, Projectile.position.Y - Projectile.velocity.Y);
        int num41 = Dust.NewDust(projectilePos, Projectile.width, Projectile.height, 15, 0f, 0f, 100, default, 2f);
        Main.dust[num41].noGravity = true;
        Main.dust[num41].velocity *= 2f;
        Dust.NewDust(projectilePos, Projectile.width, Projectile.height, 15, 0f, 0f, 100, default, 1f);

        if (Projectile.owner == Main.myPlayer)
        {
            if (Main.netMode != NetmodeID.SinglePlayer)
            {
                NetMessage.SendData(MessageID.KillProjectile, -1, -1, null, Projectile.identity, (float)Projectile.owner, 0f, 0f, 0);
            }
        }
        Projectile.active = false;
    }

    //This is too hard to see especially at night, so i'm making it ignore all lighting and always draw at full brightness
    static Texture2D texture = (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Projectiles/Ice1Ball");
    public override bool PreDraw(ref Color lightColor)
    {
        if (texture == null || texture.IsDisposed)
        {
            texture = (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Projectiles/Ice1Ball");
        }
        SpriteEffects spriteEffects = SpriteEffects.None;
        if (Projectile.spriteDirection == -1)
        {
            spriteEffects = SpriteEffects.FlipHorizontally;
        }
        //Get the premultiplied, properly transparent texture
        int frameHeight = ((Texture2D)Terraria.GameContent.TextureAssets.Projectile[Projectile.type]).Height / Main.projFrames[Projectile.type];
        int startY = frameHeight * Projectile.frame;
        Rectangle sourceRectangle = new Rectangle(0, startY, texture.Width, frameHeight);
        Vector2 origin = sourceRectangle.Size() / 2f;
        Main.EntitySpriteDraw(texture,
            Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY),
            sourceRectangle, Color.White, Projectile.rotation, origin, Projectile.scale, spriteEffects, 0);

        return false;
    }
}
