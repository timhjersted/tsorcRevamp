using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Prime
{
    class PrimeSaw : ModProjectile
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
            Projectile.timeLeft = 9999;
            Projectile.scale = 4;
        }

        int timer = 0;
        public override void AI()
        {
            timer++;
            if (timer > 400)
            {
                Projectile.tileCollide = false;
                Projectile.damage = 0;

                if (Main.npc[(int)Projectile.ai[1]].active)
                {
                    if (Projectile.Distance(Main.npc[(int)Projectile.ai[1]].Center) > 50)
                    {
                        UsefulFunctions.SmoothHoming(Projectile, Main.npc[(int)Projectile.ai[1]].Center, 1, 50);
                    }
                    else
                    {
                        Projectile.Kill();
                    }
                }
                else
                {
                    Projectile.Kill();
                }
            }
        }
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            //Allow the projectile to bounce
            Collision.HitTiles(Projectile.position + Projectile.velocity, Projectile.velocity, Projectile.width, Projectile.height);
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item70, Projectile.Center);

            Player closest = UsefulFunctions.GetClosestPlayer(Projectile.Center);
            if (closest != null)
            {
                Projectile.velocity = UsefulFunctions.Aim(Projectile.Center, closest.Center, 25);
            }
            else
            {
                if (Projectile.velocity.X != oldVelocity.X)
                {
                    Projectile.velocity.X = -oldVelocity.X;
                }
                if (Projectile.velocity.Y != oldVelocity.Y)
                {
                    Projectile.velocity.Y = -oldVelocity.Y;
                }
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
}
