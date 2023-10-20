using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
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
            Projectile.penetrate = -1;
            Projectile.width = 58;
            Projectile.height = 58;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
            Projectile.hostile = true;
            Projectile.timeLeft = 9999;
            Projectile.scale = 1.5f;
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

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.OnFire, 300);
        }

        public override bool PreKill(int timeLeft)
        {
            return base.PreKill(timeLeft);
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            //Allow the projectile to bounce
            Collision.HitTiles(Projectile.position + Projectile.velocity, Projectile.velocity, Projectile.width, Projectile.height);
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item70, Projectile.Center);

            Player closest = UsefulFunctions.GetClosestPlayer(Projectile.Center);
            if (closest != null)
            {
                Projectile.velocity = UsefulFunctions.Aim(Projectile.Center, closest.Center, 15);
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



        public static Texture2D sawTexture;
        public static ArmorShaderData data;
        public override bool PreDraw(ref Color lightColor)
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            UsefulFunctions.EnsureLoaded(ref sawTexture, "tsorcRevamp/NPCs/Bosses/PrimeV2/PrimeSawBlade");

            Projectile.frameCounter++;
            if (Projectile.frameCounter == 2)
            {
                Projectile.frameCounter = 0;
                Projectile.frame++;
            }
            if (Projectile.frame >= 4)
            {
                Projectile.frame = 0;
            }

            if (data == null)
            {
                data = GameShaders.Armor.GetSecondaryShader((byte)GameShaders.Armor.GetShaderIdFromItemId(ItemID.SolarDye), Main.LocalPlayer);
            }


            data.UseColor(Color.OrangeRed);
            data.Apply(null);

            lightColor = Color.White;


            int frameHeight = sawTexture.Height / 4;
            int startY = frameHeight * Projectile.frame;
            Rectangle sourceRectangle = new Rectangle(0, startY, sawTexture.Width, frameHeight);

            // Redraw the projectile with the color not influenced by light
            Vector2 drawOrigin = new Vector2(sawTexture.Width * 0.5f, sawTexture.Height * 0.25f);
            int shadowFrame = Projectile.frame;
            for (int k = Projectile.oldPos.Length - 1; k >= 0; k--)
            {
                Vector2 drawPos = (Projectile.oldPos[k] - Main.screenPosition + Projectile.Size / 2f);
                Rectangle shadowRectangle = new Rectangle(0, frameHeight * shadowFrame, sawTexture.Width, frameHeight);
                Color color = Projectile.GetAlpha(lightColor) * ((Projectile.oldPos.Length - k) / (float)Projectile.oldPos.Length);
                Main.EntitySpriteDraw(sawTexture, drawPos + new Vector2(0, 44), shadowRectangle, color, 0, drawOrigin, Projectile.scale, SpriteEffects.None, 0);

                shadowFrame--;
                if (shadowFrame < 0)
                {
                    shadowFrame = 3;
                }
            }

            Main.EntitySpriteDraw(sawTexture, Projectile.Center - Main.screenPosition + new Vector2(0, 44), sourceRectangle, lightColor, 0, drawOrigin, Projectile.scale, SpriteEffects.None, 0);

            UsefulFunctions.RestartSpritebatch(ref Main.spriteBatch);

            return false;
        }
    }
}
