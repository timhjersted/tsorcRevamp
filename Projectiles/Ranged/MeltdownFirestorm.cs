using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Enums;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Ranged
{
    class MeltdownFirestorm : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.hostile = false;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.light = 1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }
        public override string Texture => "tsorcRevamp/Projectiles/Enemy/Triad/HomingStarStar";
        Player owner
        {
            get
            {
                return Main.player[Projectile.owner];
            }
        }

        bool dying = false;
        float size = 0;
        Vector2 truePosition;
        float maxSize = 2700;
        bool initialized = false;
        float fadeIn;
        float trueSize = 1;
        public override void AI()
        {
            Projectile.timeLeft = 2;

            if (dying)
            {
                fadeIn--;
                if(fadeIn <= 0)
                {
                    Projectile.Kill();
                    return;
                }
            }


            if (owner.whoAmI == Main.myPlayer)
            {
                Item dummyItem = new Item(ModContent.ItemType<Items.Weapons.Ranged.Flamethrowers.Meltdown>());
                Item gelStack = owner.ChooseAmmo(dummyItem);
                if (gelStack == null)
                {
                    dying = true;
                    return;
                }
                else
                {

                    Projectile.rotation = UsefulFunctions.Aim(owner.Center, Main.MouseWorld, 1).ToRotation();
                    Projectile.direction = Main.MouseWorld.X > owner.Center.X ? 1 : -1;
                    Vector2 rotDir = Projectile.rotation.ToRotationVector2();
                    Projectile.Center = owner.Center + rotDir * 30;
                    truePosition = owner.Center + rotDir * 30;
                    if (Projectile.direction == -1)
                    {
                        rotDir *= -1;
                    }
                    owner.itemRotation = rotDir.ToRotation();
                    owner.ChangeDir(Projectile.direction);
                    owner.itemTime = 2; // Set item time to 2 frames while we are used
                    owner.itemAnimation = 2; // Set item animation time to 2 frames while we are used

                    if (Main.GameUpdateCount % 60 == 0)
                    {
                        gelStack.stack--;
                        if (gelStack.stack <= 0)
                        {
                            Projectile.damage = 0;
                            dying = true;
                            gelStack.TurnToAir();
                        }
                    }
                }
            }



            float newTrueSize = (float)Math.Pow((UsefulFunctions.GetFirstCollision(Projectile.Center, Projectile.rotation.ToRotationVector2(), ignoreNPCs: true) - Projectile.Center).Length() / 600f, 0.5f);
            if(newTrueSize < trueSize)
            {
                trueSize = newTrueSize;
            }
            else if(newTrueSize > trueSize + 0.01)
            {
                trueSize += 0.01f;
            }
            if(trueSize > 1)
            {
                trueSize = 1;
            }

            Vector2 unit = Projectile.rotation.ToRotationVector2();
            Vector2 endpoint = Projectile.Center + trueSize * (unit * (400 + (size / 6f)));
            DelegateMethods.v3_1 = Color.OrangeRed.ToVector3();
            Utils.PlotTileLine(Projectile.Center, endpoint, 32, DelegateMethods.CastLight);
            DelegateMethods.tilecut_0 = TileCuttingContext.AttackProjectile;
            DelegateMethods.tileCutIgnore = TileID.Sets.TileCutIgnore.None;
            Utils.PlotTileLine(Projectile.Center, endpoint, 32, DelegateMethods.CutTiles);


            if (!owner.channel || owner.noItems || owner.CCed)
            {
                Projectile.damage = 0;
                dying = true;
                return;
            }

            if (fadeIn < 30)
            {
                fadeIn++;
                return;
            }

            if (size < maxSize)
            {
                size += 10f;            
            }
        }

        //Custom collision
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float distance = Vector2.Distance(truePosition, targetHitbox.Center.ToVector2());
            float angleBetween = (float)UsefulFunctions.CompareAngles(Vector2.Normalize(truePosition - targetHitbox.Center.ToVector2()), Projectile.rotation.ToRotationVector2());
            return distance < trueSize * (600 + (size / 6f)) && Math.Abs(angleBetween - MathHelper.Pi) < angle / 2.85f;
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

            angle = MathHelper.TwoPi / 10f;
            float shaderRotation = 0;
            shaderRotation %= MathHelper.TwoPi;
            effect.Parameters["splitAngle"].SetValue(angle);
            effect.Parameters["rotation"].SetValue(shaderRotation);
            effect.Parameters["time"].SetValue((float)Main.timeForVisualEffects / 252);
            effect.Parameters["length"].SetValue(.05f * size / maxSize);
            float opacity = 1;
            
            if(fadeIn < 30)
            {
                MathHelper.Lerp(0.01f, 1, fadeIn / 30f);
                opacity *= fadeIn / 30f;
                opacity *= fadeIn / 30f;
            }

            effect.Parameters["opacity"].SetValue(opacity * 5);

            //I precompute many values once here so that I don't have to calculate them for every single pixel in the shader. Enormous performance save.
            effect.Parameters["rotationMinusPI"].SetValue(shaderRotation - MathHelper.Pi);
            effect.Parameters["splitAnglePlusRotationMinusPI"].SetValue(shaderRotation + angle - MathHelper.Pi);
            effect.Parameters["RotationMinus2PIMinusSplitAngleMinusPI"].SetValue((shaderRotation - (MathHelper.TwoPi - angle)) - MathHelper.Pi);

            //Apply the shader
            effect.CurrentTechnique.Passes[0].Apply();

            Rectangle recsize = new Rectangle(0, 0, tsorcRevamp.NoiseTurbulent.Width, tsorcRevamp.NoiseTurbulent.Height);
            Vector2 origin = new Vector2(recsize.Width * 0.5f, recsize.Height * 0.5f);

            //Draw the rendertarget with the shader
            Main.spriteBatch.Draw(tsorcRevamp.NoiseTurbulent, truePosition - Main.screenPosition, recsize, Color.White, Projectile.rotation + (MathHelper.Pi - angle / 2f), origin, trueSize * trueSize * 7.5f, SpriteEffects.None, 0);

            //Restart the spritebatch so the shader doesn't get applied to the rest of the game
            UsefulFunctions.RestartSpritebatch(ref Main.spriteBatch);

            return false;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.OnFire, 300, false);
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire3, 300, false);
        }
    }
}