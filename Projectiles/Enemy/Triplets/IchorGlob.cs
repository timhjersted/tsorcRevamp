using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Graphics;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Triplets
{
    class IchorGlob : ModProjectile
    {
        
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Ichor Glob");
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 60;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 3;
        }
        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.scale = 1.1f;
            Projectile.timeLeft = 200;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.friendly = false;
        }
        public override string Texture => "tsorcRevamp/Projectiles/Enemy/Triplets/HomingStarStar";

        public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
        {
            target.AddBuff(BuffID.OnFire, 300);
        }


        float[] trailRotations = new float[6] { 0, 0, 0, 0, 0, 0 };
        bool playedSound = false;
        bool spawnedTrail = false;
        public override void AI()
        {
            Player target = UsefulFunctions.GetClosestPlayer(Projectile.Center);
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (!playedSound)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item43 with { Volume = 0.5f}, Projectile.Center);

                if(Projectile.ai[1] == 1)
                {
                    Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(), Projectile.Center, Projectile.velocity, ModContent.ProjectileType<Projectiles.Trails.HomingStarTrail>(), Projectile.damage, 0, Main.myPlayer, 0, Projectile.whoAmI);
                    Projectile.timeLeft = 400;
                }
                playedSound = true;
            }

            //Default homing strength
            float homingAcceleration = 0.15f;

            //Accelerate downwards, do not despawn until impact
            if (Projectile.ai[0] == 1)
            {
                Projectile.timeLeft = 100;
                if (Projectile.velocity.Y < 14f)
                {
                    Projectile.velocity.Y += 1f;
                }
                homingAcceleration = 0;
                if(Projectile.Center.Y < target.Center.Y)
                {
                    Projectile.tileCollide = false;
                }
                else
                {
                    Projectile.tileCollide = true;
                }
            }
            
            //No homing
            if (Projectile.ai[0] == 2)
            {
                homingAcceleration = 0;
            }

            //Perform homing
            if (target != null)
            {
                UsefulFunctions.SmoothHoming(Projectile, target.Center, homingAcceleration, 30, target.velocity, false);
            }
        }

        public override void Kill(int timeLeft)
        {
            Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit3 with { Volume = 0.5f}, Projectile.Center);
        }

        float Progress(float progress)
        {
            float percent = 1f;
            float lerpValue = Utils.GetLerpValue(0f, 0.6f, progress, clamped: true);
            percent *= 1f - (1f - lerpValue) * (1f - lerpValue);
            return MathHelper.Lerp(0f, 30f, percent);
        }

        Color ColorValue(float progress)
        {
            float timeFactor = (float)Math.Sin(Math.Abs(progress - Main.GlobalTimeWrappedHourly * 3));
            Color result = Color.Lerp(Color.Cyan, Color.DeepPink, (timeFactor + 1f) / 2f);
            //Main.NewText(timeFactor + 1);
            //result = ;
            result.A = 0;

            return result;
        }
        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            behindProjectiles.Add(index);
        }

        Texture2D texture;
        Texture2D starTexture;
        float starRotation;
        public override bool PreDraw(ref Color lightColor)
        {
            if (Projectile.ai[1] == 1)
            {
                return false;
            }
            if (texture == null || texture.IsDisposed)
            {
                texture = (Texture2D)ModContent.Request<Texture2D>(Texture, ReLogic.Content.AssetRequestMode.ImmediateLoad);
            }
            if (starTexture == null || starTexture.IsDisposed)
            {
                starTexture = (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Projectiles/Enemy/Triplets/HomingStarStar", ReLogic.Content.AssetRequestMode.ImmediateLoad);
            }

            Rectangle sourceRectangle = new Rectangle(0, 0, texture.Width, texture.Height);
            Rectangle starSourceRectangle = new Rectangle(0, 0, starTexture.Width, starTexture.Height);
            Vector2 origin = sourceRectangle.Size() / 2f;
            origin.Y += 20;
            Vector2 starOrigin = starSourceRectangle.Size() / 2f;
            DrawOriginOffsetY = 100;

            Vector2 offset = Projectile.position - Projectile.Center;
            //Draw shadow trails
            for (float i = 5; i >= 0; i--)
            {
                Main.spriteBatch.Draw(texture, Projectile.oldPos[(int)i * 2] - Main.screenPosition - offset, sourceRectangle, Color.Cyan * ((6 - i) / 6), Projectile.oldRot[(int)i * 2] - MathHelper.PiOver2, origin, Projectile.scale, SpriteEffects.None, 0);
            }
            Main.spriteBatch.Draw(texture, Projectile.Center - Main.screenPosition, sourceRectangle, Color.White, Projectile.rotation - MathHelper.PiOver2, origin, Projectile.scale, SpriteEffects.None, 0);
            Vector2 starOffset = Projectile.velocity;
            starOffset.Normalize();
            if (Projectile.ai[1] != 1)
            {
                Main.spriteBatch.Draw(starTexture, Projectile.Center - Main.screenPosition, starSourceRectangle, Color.White, Projectile.rotation + starRotation, starOrigin, Projectile.scale * 0.75f, SpriteEffects.None, 0);
            }
            starRotation += 0.1f;
            return false;
        }
    }
}
