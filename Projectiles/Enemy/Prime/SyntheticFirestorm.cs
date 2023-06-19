using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Marilith
{
    class SyntheticFirestorm : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Cataclysmic Firestorm");
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
        }
        public override string Texture => "tsorcRevamp/Projectiles/Enemy/Triad/HomingStarStar";

        float size = 0;
        Vector2 truePosition;
        float maxSize = 1200;
        float explosionTime = 4000;
        bool initialized = false;
        float fadeIn;
        public override void AI()
        {
            int? index = UsefulFunctions.GetFirstNPC(ModContent.NPCType<NPCs.Bosses.PrimeV2.TheMachine>());
            if (index != null)
            {
                truePosition = Main.npc[index.Value].Center;
            }
            else
            {
                Projectile.active = false;
            }

            if (!initialized)
            {
                initialized = true;
                if(Projectile.ai[0] == 0)
                {
                    Projectile.rotation = -MathHelper.PiOver2;
                }
                for(int i = 0; i < Main.maxProjectiles; i++)
                {
                    if (Main.projectile[i].active && Main.projectile[i].type == Projectile.type && Main.projectile[i].ai[0] == Projectile.ai[0] - 1)
                    Projectile.rotation = Main.projectile[i].rotation + MathHelper.TwoPi / 3f;
                }
            }

            Projectile.rotation -= 0.005f;

            if (fadeIn < 120)
            {
                fadeIn++;
                if(fadeIn == 110)
                {
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Roar, truePosition);
                }
                return;
            }
            if(Projectile.rotation > MathHelper.TwoPi)
            {
                Projectile.rotation -= MathHelper.TwoPi;
            }
            if(Projectile.rotation < 0)
            {
                Projectile.rotation += MathHelper.TwoPi;
            }
            
            if(truePosition == Vector2.Zero)
            {
                truePosition = Projectile.Center;
            }

            Projectile.Center = Main.LocalPlayer.Center;
            Projectile.timeLeft = 2;


            UsefulFunctions.DustRing(truePosition, size + 600, DustID.ShadowbeamStaff, 20, 3);
            if (size < maxSize)
            {
                size += 10f;            
            }
        }
        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            //behindNPCsAndTiles.Add(index);
        }

        //Custom collision
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if(fadeIn < 120)
            {
                return false;
            }
            float distance = Vector2.Distance(truePosition, Main.LocalPlayer.Center);
            float angleBetween = (float)UsefulFunctions.CompareAngles(Vector2.Normalize(truePosition - Main.LocalPlayer.Center), Projectile.rotation.ToRotationVector2());
            return distance < size && Math.Abs(angleBetween - MathHelper.Pi) < angle / 2.85f;
        }

        public static Effect effect;
        public float angle;
        public override bool PreDraw(ref Color lightColor)
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            if (effect == null)
            {
                effect = ModContent.Request<Effect>("tsorcRevamp/Effects/SyntheticFirestorm", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
                //effect = new Effect(Main.graphics.GraphicsDevice, Mod.GetFileBytes("Effects/SyntheticFirestorm"));
            }

            angle = MathHelper.TwoPi / 6f;
            float shaderRotation = Projectile.rotation + (MathHelper.Pi - angle / 2f);
            shaderRotation %= MathHelper.TwoPi;
            effect.Parameters["splitAngle"].SetValue(angle);
            effect.Parameters["rotation"].SetValue(shaderRotation);
            effect.Parameters["time"].SetValue((float)Main.timeForVisualEffects / 312);
            effect.Parameters["length"].SetValue(.35f * size / maxSize);
            float opacity = 1;
            if (fadeIn < 110)
            {
                opacity = 0.1f;
            }
            else if(fadeIn < 120)
            {
                MathHelper.Lerp(0.01f, 1, (fadeIn - 110f) / 10f);
            }

            effect.Parameters["opacity"].SetValue(opacity);

            //I precompute many values once here so that I don't have to calculate them for every single pixel in the shader. Enormous performance save.
            effect.Parameters["rotationMinusPI"].SetValue(shaderRotation - MathHelper.Pi);
            effect.Parameters["splitAnglePlusRotationMinusPI"].SetValue(shaderRotation + angle - MathHelper.Pi);
            effect.Parameters["RotationMinus2PIMinusSplitAngleMinusPI"].SetValue((shaderRotation - (MathHelper.TwoPi - angle)) - MathHelper.Pi);

            //Apply the shader
            effect.CurrentTechnique.Passes[0].Apply();

            Rectangle recsize = new Rectangle(0, 0, tsorcRevamp.NoiseTurbulent.Width, tsorcRevamp.NoiseTurbulent.Height);
            Vector2 origin = new Vector2(recsize.Width * 0.5f, recsize.Height * 0.5f);

            //Draw the rendertarget with the shader
            Main.spriteBatch.Draw(tsorcRevamp.NoiseTurbulent, truePosition - Main.screenPosition, recsize, Color.White, 0, origin, 4.5f, SpriteEffects.None, 0);

            //Restart the spritebatch so the shader doesn't get applied to the rest of the game
            UsefulFunctions.RestartSpritebatch(ref Main.spriteBatch);

            return false;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.OnFire, 300, false);
        }
    }
}