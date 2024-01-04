using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Birbs
{
    class SorrowFrozenBreath : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            //Always draw this projectile even if its "center" is far offscreen
            ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 99999999;
        }

        public override void SetDefaults()
        {
            Projectile.width = 194;
            Projectile.height = 194;
            DrawOriginOffsetX = -96;
            DrawOriginOffsetY = 94;
            Main.projFrames[Projectile.type] = 7;
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.scale = 2;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.light = 1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 120;
        }
        public override string Texture => "tsorcRevamp/Projectiles/Enemy/Triad/HomingStarStar";

        float size = 0;
        Vector2 truePosition;
        float maxSize = 200;
        float fadeIn;
        int? hostIndex;
        bool initialized = false;
        public override void AI()
        {
            maxSize = 150;
            if (!initialized)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Roar, Projectile.Center);
                hostIndex = UsefulFunctions.GetFirstNPC(ModContent.NPCType<NPCs.Bosses.TheSorrow>());
                Projectile.rotation = Projectile.velocity.ToRotation();
                Projectile.velocity = Vector2.Zero;
                Projectile.timeLeft = (int)Projectile.ai[0] + 15;

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Projectile.velocity, ModContent.ProjectileType<Projectiles.VFX.ExplosionFlash>(), 0, 0, Main.myPlayer, 400, 30);
                }
                initialized = true;
            }
            if (hostIndex != null)
            {
                Projectile.Center = Main.npc[hostIndex.Value].Center;
                Projectile.position.X -= 7;
                Projectile.position.Y -= 10;

                Vector2 targetRotation = UsefulFunctions.Aim(Projectile.Center, Main.player[Main.npc[hostIndex.Value].target].Center, 1);
                Vector2 currentRotation = Projectile.rotation.ToRotationVector2();
                Vector2 nextRotationVector = Vector2.Lerp(currentRotation, targetRotation, 0.025f);
                Projectile.rotation = MathHelper.WrapAngle(nextRotationVector.ToRotation());
                if (Projectile.rotation < 0)
                {
                    Projectile.rotation += MathHelper.TwoPi;
                }
            }
            else
            {
                Projectile.active = false;
            }

            if (size < maxSize)
            {
                size += 2;
            }
        }
        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            overPlayers.Add(index);
        }

        //Custom collision
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (Projectile.timeLeft < 15)
            {
                return false;
            }

            float distance = Vector2.Distance(Projectile.Center, Main.LocalPlayer.Center);
            float angleBetween = (float)UsefulFunctions.CompareAngles(Vector2.Normalize(Projectile.Center - targetHitbox.Center.ToVector2()), Projectile.rotation.ToRotationVector2());
            //Main.NewText("Size " + distance  + " / " + size * 2.7f);
            //Main.NewText("Angle " + (Math.Abs(angleBetween - MathHelper.Pi)) + " / " + angle / 3.86f);
            return distance < size * 2.7f && Math.Abs(angleBetween - MathHelper.Pi) < angle / 3.86f;
        }

        public static Effect effect;
        public float angle;
        public override bool PreDraw(ref Color lightColor)
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            if (effect == null)
            {
                effect = ModContent.Request<Effect>("tsorcRevamp/Effects/SyntheticBlizzard", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            }

            angle = MathHelper.TwoPi / 8f;
            float shaderRotation = Projectile.rotation + (MathHelper.Pi - angle / 2f);
            shaderRotation %= MathHelper.TwoPi;
            effect.Parameters["splitAngle"].SetValue(angle);
            effect.Parameters["rotation"].SetValue(shaderRotation);
            effect.Parameters["time"].SetValue((float)Main.timeForVisualEffects / 252);
            effect.Parameters["length"].SetValue(.07f * size / maxSize);
            effect.Parameters["texScale"].SetValue(12);
            effect.Parameters["texScale2"].SetValue(1);
            effect.Parameters["noiseTexture"].SetValue(tsorcRevamp.NoiseTurbulent);

            float opacity = 11;
            if (Projectile.timeLeft < 45 && Projectile.timeLeft > 30)
            {
                opacity = MathHelper.Lerp(opacity, 1, (45f - Projectile.timeLeft) / 15f);
            }
            else
            {
                opacity = Projectile.timeLeft / 30f;
            }

            effect.Parameters["opacity"].SetValue(opacity);

            //I precompute many values once here so that I don't have to calculate them for every single pixel in the shader. Enormous performance save.
            effect.Parameters["rotationMinusPI"].SetValue(shaderRotation - MathHelper.Pi);
            effect.Parameters["splitAnglePlusRotationMinusPI"].SetValue(shaderRotation + angle - MathHelper.Pi);
            effect.Parameters["RotationMinus2PIMinusSplitAngleMinusPI"].SetValue((shaderRotation - (MathHelper.TwoPi - angle)) - MathHelper.Pi);

            //Apply the shader
            effect.CurrentTechnique.Passes[0].Apply();

            Rectangle recsize = new Rectangle(0, 0, tsorcRevamp.NoiseVoronoi.Width, tsorcRevamp.NoiseVoronoi.Height);
            Vector2 origin = new Vector2(recsize.Width * 0.5f, recsize.Height * 0.5f);

            //Draw the rendertarget with the shader
            Main.spriteBatch.Draw(tsorcRevamp.NoiseVoronoi, Projectile.Center - Main.screenPosition, recsize, Color.White, 0, origin, 6.5f * size / maxSize, SpriteEffects.None, 0);

            //Restart the spritebatch so the shader doesn't get applied to the rest of the game
            UsefulFunctions.RestartSpritebatch(ref Main.spriteBatch);

            return false;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.Frostburn, 300, false);
        }
    }
}