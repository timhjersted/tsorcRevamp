using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace tsorcRevamp.Projectiles {

    public class SilverBall : ModProjectile {

        private const string ChainTexturePath = "tsorcRevamp/Projectiles/chain";

        public override void SetDefaults() {
            Projectile.width = 34;
            Projectile.height = 34;
            Projectile.scale = 0.8f;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.DamageType = DamageClass.Melee;
        }

        public override void AI() {

            var player = Main.player[Projectile.owner];

            if (player.dead) {
                Projectile.Kill();
                return;
            }

            player.itemAnimation = 10;
            player.itemTime = 10;

            int newDirection = Projectile.Center.X > player.Center.X ? 1 : -1;
            player.ChangeDir(newDirection);
            Projectile.direction = newDirection;

            var vectorToPlayer = player.MountedCenter - Projectile.Center;
            float currentChainLength = vectorToPlayer.Length();

            // ai[0] == 0: being thrown out
            // ai[0] == 1: Flail has hit a tile or has reached maxChainLength, and is now swinging
            // ai[1] == 1 or !projectile.tileCollide: forced retraction


            if (Projectile.ai[0] == 0f) {
                float maxChainLength = 160f; //pixels
                Projectile.tileCollide = true;
                if (currentChainLength > maxChainLength) {
                    // If we reach maxChainLength, we change behavior.
                    Projectile.ai[0] = 1f;
                    Projectile.netUpdate = true;
                }
                else if (!player.channel) { //release mouse

                    if (Projectile.velocity.Y < 0f)
                        Projectile.velocity.Y *= 0.9f;

                    Projectile.velocity.Y += 1f;
                    Projectile.velocity.X *= 0.9f;
                }
            }
            else if (Projectile.ai[0] == 1f) {
                float elasticFactorA = 14f / player.meleeSpeed;
                float elasticFactorB = 0.9f / player.meleeSpeed;
                float maxStretchLength = 300f; //flails force retract, even through walls, when they reach this length

                if (Projectile.ai[1] == 1f)
                    Projectile.tileCollide = false;

                if (!player.channel || currentChainLength > maxStretchLength || !Projectile.tileCollide) {
                    Projectile.ai[1] = 1f;

                    if (Projectile.tileCollide)
                        Projectile.netUpdate = true;

                    Projectile.tileCollide = false;

                    if (currentChainLength < 20f)
                        Projectile.Kill();
                }

                if (!Projectile.tileCollide)
                    elasticFactorB *= 2f;

                int restingChainLength = 60;


                if (currentChainLength > restingChainLength || !Projectile.tileCollide) {
                    var elasticAcceleration = vectorToPlayer * elasticFactorA / currentChainLength - Projectile.velocity;
                    elasticAcceleration *= elasticFactorB / elasticAcceleration.Length();
                    Projectile.velocity *= 0.98f;
                    Projectile.velocity += elasticAcceleration;
                }
                else {

                    if (Math.Abs(Projectile.velocity.X) + Math.Abs(Projectile.velocity.Y) < 6f) {
                        Projectile.velocity.X *= 0.96f;
                        Projectile.velocity.Y += 0.2f;
                    }
                    if (player.velocity.X == 0f)
                        Projectile.velocity.X *= 0.96f;
                }
            }

            Projectile.rotation = vectorToPlayer.ToRotation() - Projectile.velocity.X * 0.1f;

            //add shoot projectiles here (like flower pow)
        }

        public override bool OnTileCollide(Vector2 oldVelocity) {
            //slow when bouncing
            bool shouldMakeSound = false;

            if (oldVelocity.X != Projectile.velocity.X) {
                if (Math.Abs(oldVelocity.X) > 4f) {
                    shouldMakeSound = true;
                }

                Projectile.position.X += Projectile.velocity.X;
                Projectile.velocity.X = -oldVelocity.X * 0.2f;
            }

            if (oldVelocity.Y != Projectile.velocity.Y) {
                if (Math.Abs(oldVelocity.Y) > 4f) {
                    shouldMakeSound = true;
                }

                Projectile.position.Y += Projectile.velocity.Y;
                Projectile.velocity.Y = -oldVelocity.Y * 0.2f;
            }
            Projectile.ai[0] = 1f;

            if (shouldMakeSound) {
                Projectile.netUpdate = true;
                Collision.HitTiles(Projectile.position, Projectile.velocity, Projectile.width, Projectile.height);
                Main.PlaySound(SoundID.Dig, (int)Projectile.position.X, (int)Projectile.position.Y);
            }

            return false;
        }

        static Texture2D chainTexture = ModContent.GetTexture(ChainTexturePath);
        public override bool PreDraw(ref Color lightColor)
        {
            if (chainTexture == null || chainTexture.IsDisposed)
            {
                chainTexture = ModContent.GetTexture(ChainTexturePath);
            }
            var player = Main.player[Projectile.owner];

            Vector2 mountedCenter = player.MountedCenter;

            var drawPosition = Projectile.Center;
            var remainingVectorToPlayer = mountedCenter - drawPosition;

            float rotation = remainingVectorToPlayer.ToRotation() - MathHelper.PiOver2;

            if (Projectile.alpha == 0) {
                int direction = -1;

                if (Projectile.Center.X < mountedCenter.X)
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
                Main.EntitySpriteDraw(chainTexture, drawPosition - Main.screenPosition, null, color, rotation, chainTexture.Size() * 0.5f, 1f, SpriteEffects.None, 0f);
            }

            return true;
        }
    }
}
