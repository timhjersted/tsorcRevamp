using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using Terraria.Graphics.Shaders;

namespace tsorcRevamp.Projectiles {

    public class HeavenBall : ModProjectile {

        private const string ChainTexturePath = "tsorcRevamp/Projectiles/Chain";

        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Heaven Ball");
        }

        public override void SetDefaults() {
            projectile.width = 30;
            projectile.height = 30;
            projectile.friendly = true;
            projectile.penetrate = -1;
            projectile.melee = true;
        }

        public override void AI() {

            var dust = Dust.NewDustDirect(projectile.position, projectile.width, projectile.height, 171, projectile.velocity.X * 0.4f, projectile.velocity.Y * 0.4f, 0, new Color(184, 255, 0), 1f);
            dust.shader = GameShaders.Armor.GetSecondaryShader(47, Main.LocalPlayer);
            dust.noGravity = true;
            dust.velocity /= 2f;

            var player = Main.player[projectile.owner];

            if (player.dead) {
                projectile.Kill();
                return;
            }

            player.itemAnimation = 10;
            player.itemTime = 10;

            int newDirection = projectile.Center.X > player.Center.X ? 1 : -1;
            player.ChangeDir(newDirection);
            projectile.direction = newDirection;

            var vectorToPlayer = player.MountedCenter - projectile.Center;
            float currentChainLength = vectorToPlayer.Length();

            // ai[0] == 0: being thrown out
            // ai[0] == 1: Flail has hit a tile or has reached maxChainLength, and is now swinging
            // ai[1] == 1 or !projectile.tileCollide: forced retraction


            if (projectile.ai[0] == 0f) {
                float maxChainLength = 160f; //pixels
                projectile.tileCollide = true;
                if (currentChainLength > maxChainLength) {
                    // If we reach maxChainLength, we change behavior.
                    projectile.ai[0] = 1f;
                    projectile.netUpdate = true;
                }
                else if (!player.channel) { //release mouse

                    if (projectile.velocity.Y < 0f)
                        projectile.velocity.Y *= 0.9f;

                    projectile.velocity.Y += 1f;
                    projectile.velocity.X *= 0.9f;
                }
            }
            else if (projectile.ai[0] == 1f) {
                float elasticFactorA = 14f / player.meleeSpeed;
                float elasticFactorB = 0.9f / player.meleeSpeed;
                float maxStretchLength = 300f; //flails force retract, even through walls, when they reach this length

                if (projectile.ai[1] == 1f)
                    projectile.tileCollide = false;

                if (!player.channel || currentChainLength > maxStretchLength || !projectile.tileCollide) {
                    projectile.ai[1] = 1f;

                    if (projectile.tileCollide)
                        projectile.netUpdate = true;

                    projectile.tileCollide = false;

                    if (currentChainLength < 20f)
                        projectile.Kill();
                }

                if (!projectile.tileCollide)
                    elasticFactorB *= 2f;

                int restingChainLength = 60;


                if (currentChainLength > restingChainLength || !projectile.tileCollide) {
                    var elasticAcceleration = vectorToPlayer * elasticFactorA / currentChainLength - projectile.velocity;
                    elasticAcceleration *= elasticFactorB / elasticAcceleration.Length();
                    projectile.velocity *= 0.98f;
                    projectile.velocity += elasticAcceleration;
                }
                else {

                    if (Math.Abs(projectile.velocity.X) + Math.Abs(projectile.velocity.Y) < 6f) {
                        projectile.velocity.X *= 0.96f;
                        projectile.velocity.Y += 0.2f;
                    }
                    if (player.velocity.X == 0f)
                        projectile.velocity.X *= 0.96f;
                }
            }

            projectile.rotation = vectorToPlayer.ToRotation() - projectile.velocity.X * 0.1f;

            //add shoot projectiles here (like flower pow)
        }

        public override bool OnTileCollide(Vector2 oldVelocity) {
            //slow when bouncing
            bool shouldMakeSound = false;

            if (oldVelocity.X != projectile.velocity.X) {
                if (Math.Abs(oldVelocity.X) > 4f) {
                    shouldMakeSound = true;
                }

                projectile.position.X += projectile.velocity.X;
                projectile.velocity.X = -oldVelocity.X * 0.2f;
            }

            if (oldVelocity.Y != projectile.velocity.Y) {
                if (Math.Abs(oldVelocity.Y) > 4f) {
                    shouldMakeSound = true;
                }

                projectile.position.Y += projectile.velocity.Y;
                projectile.velocity.Y = -oldVelocity.Y * 0.2f;
            }
            projectile.ai[0] = 1f;

            if (shouldMakeSound) {
                projectile.netUpdate = true;
                Collision.HitTiles(projectile.position, projectile.velocity, projectile.width, projectile.height);
                Main.PlaySound(SoundID.Dig, (int)projectile.position.X, (int)projectile.position.Y);
            }

            return false;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor) {
            var player = Main.player[projectile.owner];

            Vector2 mountedCenter = player.MountedCenter;
            Texture2D chainTexture = ModContent.GetTexture(ChainTexturePath);

            var drawPosition = projectile.Center;
            var remainingVectorToPlayer = mountedCenter - drawPosition;

            float rotation = remainingVectorToPlayer.ToRotation() - MathHelper.PiOver2;

            if (projectile.alpha == 0) {
                int direction = -1;

                if (projectile.Center.X < mountedCenter.X)
                    direction = 1;

                player.itemRotation = (float)Math.Atan2(remainingVectorToPlayer.Y * direction, remainingVectorToPlayer.X * direction);
            }

            //draw the chain
            while (true) {
                float length = remainingVectorToPlayer.Length();

                if (length < 25f || float.IsNaN(length))
                    break;

                //12 is height of chain image
                drawPosition += remainingVectorToPlayer * 12 / length;
                remainingVectorToPlayer = mountedCenter - drawPosition;

                // Finally, we draw the texture at the coordinates using the lighting information of the tile coordinates of the chain section
                Color color = Lighting.GetColor((int)drawPosition.X / 16, (int)(drawPosition.Y / 16f));
                spriteBatch.Draw(chainTexture, drawPosition - Main.screenPosition, null, color, rotation, chainTexture.Size() * 0.5f, 1f, SpriteEffects.None, 0f);
            }

            return true;
        }
    }
}
