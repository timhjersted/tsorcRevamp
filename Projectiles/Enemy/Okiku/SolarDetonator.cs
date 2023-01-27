using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Okiku
{
    public class SolarDetonator : ModProjectile
    {

        public override void SetDefaults()
        {
            Projectile.aiStyle = 0;
            Projectile.height = 16;
            Projectile.scale = 1.2f;
            Projectile.tileCollide = false;
            Projectile.width = 16;
            Projectile.timeLeft = DetonationTime;
            Projectile.hostile = false;
        }

        float DetonationRange = 80;
        int DetonationTime = 240;
        bool spawnedLasers = false;
        const int LASER_COUNT = 6;
        int[] pickedDirections = new int[LASER_COUNT];

        public override void AI()
        {
            Projectile.rotation++;

            if (!spawnedLasers)
            {
                spawnedLasers = true;
                for (int i = 0; i < LASER_COUNT; i++)
                {
                    bool repeat;
                    int direction;

                    //Make sure two lasers don't pick the same direction
                    do
                    {
                        repeat = false;
                        direction = Main.rand.Next(8);
                        for (int j = 0; j < i; j++)
                        {
                            if (pickedDirections[j] == direction)
                            {
                                repeat = true;
                            }
                        }
                    } while (repeat == true);

                    pickedDirections[i] = direction;

                    Vector2 fireDirection = Vector2.Zero;

                    switch (direction)
                    {
                        case 0:
                            fireDirection = new Vector2(0, 1);
                            break;

                        case 1:
                            fireDirection = new Vector2(1, 0);
                            break;

                        case 2:
                            fireDirection = new Vector2(0, -1);
                            break;

                        case 3:
                            fireDirection = new Vector2(-1, 0);
                            break;

                        case 4:
                            fireDirection = new Vector2(1, 1);
                            break;

                        case 5:
                            fireDirection = new Vector2(1, -1);
                            break;

                        case 6:
                            fireDirection = new Vector2(-1, 1);
                            break;

                        case 7:
                            fireDirection = new Vector2(-1, -1);
                            break;
                    }

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(), Projectile.position, fireDirection, ModContent.ProjectileType<Projectiles.Enemy.Okiku.SolarBeam>(), Projectile.damage, .5f, Main.myPlayer, DetonationTime, UsefulFunctions.EncodeID(Projectile));
                    }
                }
            }


            //The closer it gets to detonating the more dust it spawns, up to 10 per frame
            //Note: Integer division
            for (int i = 0; i < (int)(10 * ((float)((float)DetonationTime - (float)Projectile.timeLeft) / (float)DetonationTime)); i++)
            {
                Vector2 dustOffset = Main.rand.NextVector2CircularEdge(DetonationRange, DetonationRange);
                Vector2 dustVel = Main.rand.NextVector2CircularEdge(8, 8);
                Vector2 dustPos = dustOffset + Projectile.Center;
                int dustDir = dustOffset.X < 0 ? -1 : 1;

                Dust.NewDustPerfect(dustPos, 6, new Vector2(5 * dustDir, 0), 250, Color.White, 1.0f).noGravity = true;
                Dust.NewDustPerfect(Projectile.Center, 127, dustVel, 250, Color.White, 2.0f).noGravity = true;
            }
        }

        public override bool PreKill(int timeLeft)
        {
            for (int i = 0; i < 10; i++)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile fireball = Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(), Projectile.Center, Main.rand.NextVector2Square(-18, 18), 686, Projectile.damage, .5f, Main.myPlayer);
                    fireball.Name = "Solar Detonation";
                    fireball.tileCollide = false;
                }
            }


            for (int i = 0; i < 50; i++)
            {
                Vector2 dustVel = Main.rand.NextVector2CircularEdge(8, 8);
                Dust.NewDustPerfect(Projectile.Center, 127, dustVel, 250, Color.White, 4.0f).noGravity = true;
            }
            return true;
        }

        static Texture2D texture;
        public override bool PreDraw(ref Color lightColor)
        {
            if (texture == null || texture.IsDisposed)
            {
                texture = (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Projectiles/Enemy/Okiku/SolarDetonator");
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
            //origin.X = (float)(projectile.spriteDirection == 1 ? sourceRectangle.Width - 20 : 20);
            Color drawColor = Projectile.GetAlpha(lightColor);
            Main.EntitySpriteDraw(texture,
                Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY),
                sourceRectangle, drawColor, Projectile.rotation, origin, Projectile.scale, spriteEffects, 0);

            return false;
        }
    }
}
