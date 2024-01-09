﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Melee.Spears
{
    class DarkTridentHeld : ChargedBowHeld
    {

        protected override void SetStats()
        {
            Player owner = Main.LocalPlayer;
            Item bow = owner.inventory[owner.selectedItem];
            StatModifier rangedDamage = owner.GetTotalDamage(DamageClass.Ranged);
            int bowDamage = (int)rangedDamage.ApplyTo(owner.arrowDamage.ApplyTo(bow.damage));
            minDamage = 1;
            maxDamage = bowDamage;
            minVelocity = bow.shootSpeed / 10;
            maxVelocity = bow.shootSpeed;
            chargeRate = (1f / 48f);
            Main.projFrames[Projectile.type] = 1;
            soundtype = SoundID.Item1;
        }

        protected override void Shoot()
        {
            Player player = Main.player[Projectile.owner];
            if (player.whoAmI != Main.myPlayer)
                return;

            if (player.altFunctionUse != 2)
            {
                float velocity = LerpFloat(minVelocity, maxVelocity, charge);
                Vector2 velVector = UsefulFunctions.BallisticTrajectory(Projectile.Center, Main.MouseWorld, velocity, 0.1f, false, true);
                int damage = (int)LerpFloat(minDamage, maxDamage, charge);
                Projectile.NewProjectile(player.GetSource_ItemUse(player.inventory[player.selectedItem]), Projectile.Center, velVector, ModContent.ProjectileType<DarkTridentThrown>(), damage, Projectile.knockBack, Projectile.owner, (charge >= 1) ? 1 : 0);
                if (player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse)
                {
                    //player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent -= 30f;
                }
            }
            else
            {
                Vector2 velVector = Projectile.Center - Main.MouseWorld;
                velVector.Normalize();
                velVector *= -2;

                Projectile.NewProjectile(player.GetSource_ItemUse(player.inventory[player.selectedItem]), Projectile.Center, velVector, ModContent.ProjectileType<DarkTridentPoke>(), maxDamage / 6, Projectile.knockBack, Projectile.owner);
            }
        }

        bool playedSound = false;
        protected override void UpdateAim()
        {
            Projectile.timeLeft = 2;
            Player player = Main.player[Projectile.owner];

            if (player.altFunctionUse != 2)
            {
                if (charge >= 1 && !playedSound)
                {
                    UsefulFunctions.DustRing(player.Center, 70, DustID.Torch, 60, 18);
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.5f }, player.Center);
                    playedSound = true;
                }

                Vector2 playerHandPos = player.RotatedRelativePoint(player.MountedCenter);
                playerHandPos.Y -= 15;
                Projectile.Center = player.Center + new Vector2(0, -15);
                if (player.whoAmI == Main.myPlayer)
                {
                    Projectile.velocity = UsefulFunctions.Aim(player.Center, Main.MouseWorld, 1);
                    Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
                }
                if (Projectile.velocity.X < 0)
                {
                    Projectile.direction = -1;
                }
                else
                {
                    Projectile.direction = 1;
                }
                player.heldProj = Projectile.whoAmI;
                player.itemRotation = Projectile.rotation;
                player.itemAnimation = player.itemTime = 2;

                player.ChangeDir(Projectile.direction);

                if (Projectile.owner == Main.myPlayer)
                {
                    //update character visuals while aiming
                    //Have to use maxVelocity here, because BallisticTrajectory falls back to a simple line trajectory if a shot is too far away to hit, and the transition is jarring
                    aimVector = UsefulFunctions.BallisticTrajectory(Projectile.Center, Main.MouseWorld, maxVelocity, 0.1f, false, true);
                    if (aimVector != Projectile.velocity)
                    {
                        Projectile.netUpdate = true; //update the bow visually to other players when we change aim
                    }
                    Projectile.velocity = aimVector * holdoutOffset;
                }

                Projectile.Center -= new Vector2(5 + (10 * UsefulFunctions.EasingCurve(charge)), 0).RotatedBy(Projectile.rotation - MathHelper.PiOver2);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.player[Projectile.owner].altFunctionUse != 2)
            {
                SpriteEffects spriteEffects = SpriteEffects.None;
                if (Projectile.spriteDirection == -1)
                {
                    spriteEffects = SpriteEffects.FlipHorizontally;
                }

                if (texture == null || texture.IsDisposed)
                {
                    texture = (Texture2D)ModContent.Request<Texture2D>(Projectile.ModProjectile.Texture);
                }

                int frameHeight = texture.Height / Main.projFrames[Projectile.type];
                int startY = frameHeight * Projectile.frame;
                Rectangle sourceRectangle = new Rectangle(0, startY, texture.Width, frameHeight);
                Vector2 origin = sourceRectangle.Size() / 2f;



                Main.EntitySpriteDraw(texture,
                    Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY),
                    sourceRectangle, lightColor, Projectile.rotation, origin, Projectile.scale, spriteEffects, 0);
            }
            return false;
        }

        public static Texture2D texture;

        public override void PostDraw(Color lightColor)
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            ArmorShaderData data;
            if (charge >= 1)
            {
                data = GameShaders.Armor.GetSecondaryShader((byte)GameShaders.Armor.GetShaderIdFromItemId(ItemID.SolarDye), Main.LocalPlayer);
            }
            else
            {
                data = GameShaders.Armor.GetSecondaryShader((byte)GameShaders.Armor.GetShaderIdFromItemId(ItemID.YellowDye), Main.LocalPlayer);
            }

            data.Apply(null);


            SpriteEffects spriteEffects = SpriteEffects.None;
            if (Projectile.spriteDirection == -1)
            {
                spriteEffects = SpriteEffects.FlipHorizontally;
            }

            if (texture == null || texture.IsDisposed)
            {
                texture = (Texture2D)ModContent.Request<Texture2D>(Projectile.ModProjectile.Texture);
            }

            int frameHeight = texture.Height / Main.projFrames[Projectile.type];
            int startY = frameHeight * Projectile.frame;
            Rectangle sourceRectangle = new Rectangle(0, startY, texture.Width, frameHeight);
            Vector2 origin = sourceRectangle.Size() / 2f;


            Rectangle cropped = new Rectangle((int)sourceRectangle.X, (int)sourceRectangle.Y, texture.Width, (int)(texture.Height * charge));

            Main.EntitySpriteDraw(texture,
                Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY),
                cropped, Color.White, Projectile.rotation, origin, Projectile.scale, spriteEffects, 0);

            UsefulFunctions.RestartSpritebatch(ref Main.spriteBatch);
            DrawPoints();
        }
    }
}
