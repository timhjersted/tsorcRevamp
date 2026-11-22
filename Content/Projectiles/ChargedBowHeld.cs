using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Weapons.Ranged.Bows.BowRework;

namespace tsorcRevamp.Content.Projectiles
{

    public abstract class ChargedBowHeld : ModProjectile
    {
        protected int minDamage;

        protected int maxDamage;

        protected float minVelocity;

        protected float maxVelocity;

        protected float chargeRate;

        protected SoundStyle soundtype = SoundID.Item5 with { PitchVariance = 0.2f };

        protected float holdoutOffset;

        protected bool fired;

        protected float charge;

        protected Vector2 aimVector;

        protected Texture2D pointTexture;

        protected int ammoType
        {
            get
            {
                return (int)Projectile.ai[0];
            }
            set
            {
                Projectile.ai[0] = value;
            }
        }

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Charged Bow");
            ProjectileID.Sets.NeedsUUID[Projectile.type] = true;
        }
        public sealed override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.alpha = 0;
            Projectile.timeLeft = 999999; //"ummm zeo if you hold left click for 4.6 irl hours the bow disappears!!!! please fix!!!" 
            SetStats();
            pointTexture = ModContent.Request<Texture2D>("tsorcRevamp/Textures/ChargePoint", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
        }

        protected abstract void SetStats();

        protected virtual void SpecialBehavior()
        {
        }

        protected virtual void Animate()
        {
            Projectile.frame = (int)((Main.projFrames[Projectile.type] - 1) * charge);

        }

        protected abstract void Shoot();

        // Runs once when stamina hits 0 while held, just before the projectile is removed. Bows do nothing (the
        // draw is cancelled). Spears override it to finish the attack instead: a click that overdraws into
        // stamina debt lands at 0 on the very first tick, and Dark Souls rules say that attack still goes through.
        protected virtual void OnStaminaDepleted(Player player)
        {
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.WriteVector2(aimVector);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            aimVector = reader.ReadVector2();
        }


        public sealed override void AI()
        {
            Player player = Main.player[Projectile.owner];
            Projectile.damage = 0;
            if (!player.channel)
                Projectile.Kill();

            float stamina = player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent;
            if (stamina <= 0)
            {
                if (!fired)
                {
                    OnStaminaDepleted(player);
                    fired = true;
                }
                Projectile.Kill();
                player.channel = false;
                return;
            }
            Animate();
            SpecialBehavior();
            UpdateAim();
            if (player.channel && !fired)
            {
                Projectile.timeLeft = Math.Max(Projectile.timeLeft, 2);
                if (charge < 1f)
                {
                    charge += chargeRate;
                }
            }
            else
            {
                Shoot();
                SoundEngine.PlaySound(soundtype, player.Center);
                fired = true;
            }
        }

        protected virtual void UpdateAim()
        {
            Projectile.timeLeft = 2;
            Player player = Main.player[Projectile.owner];
            Vector2 playerHandPos = player.RotatedRelativePoint(player.MountedCenter);
            Projectile.Center = new Vector2(playerHandPos.X, playerHandPos.Y);
            Projectile.rotation = Projectile.velocity.ToRotation() + (float)Math.PI / 2f;
            player.heldProj = Projectile.whoAmI;
            player.itemRotation = (Projectile.velocity * Projectile.direction).ToRotation();
            player.itemAnimation = player.itemTime = 2;
            player.ChangeDir(Projectile.direction);

            if (Projectile.owner == Main.myPlayer)
            {
                //update character visuals while aiming
                aimVector = Vector2.Normalize(Main.MouseWorld - playerHandPos);
                aimVector = Vector2.Normalize(Vector2.Lerp(Vector2.Normalize(Projectile.velocity), aimVector, 0.6f)); //taken straight from RedLaserBeam, thanks past me!
                if (aimVector != Projectile.velocity)
                {
                    Projectile.netUpdate = true; //update the bow visually to other players when we change aim
                }
                Projectile.velocity = aimVector;
            }
            Projectile.spriteDirection = Projectile.direction;

            if (ammoType > ProjectileID.None)
            {
                float clampedCharge = MathHelper.Clamp(charge, 0f, 1f);
                BowDrawAnimationPlayer.ApplyBowArmPose(
                    player,
                    Projectile.velocity,
                    clampedCharge,
                    fired,
                    0f,
                    out _,
                    out _,
                    out Player.CompositeArmStretchAmount frontStretch,
                    out float frontRotation);

                // Keep the arrow nock and bowstring vertex locked to the player's front hand so the hand draws on top of the arrow tail.
                Texture2D bowTexture = TextureAssets.Projectile[Projectile.type].Value;
                if (bowTexture != null)
                {
                    int bowFrames = Math.Max(1, Main.projFrames[Projectile.type]);
                    int bowFrameHeight = bowTexture.Height / bowFrames;
                    float pivotY = (Projectile.height * 0.5f) - DrawOriginOffsetY;
                    float stringY = MathHelper.Lerp(bowFrameHeight - 15f, bowFrameHeight - 1f, clampedCharge);
                    float stringPullback = (stringY - pivotY) * Projectile.scale;
                    Vector2 aimDirection = Projectile.velocity.SafeNormalize(Vector2.UnitX * Projectile.direction);
                    Vector2 frontHandWorld = player.GetFrontHandPosition(frontStretch, frontRotation);
                    Projectile.Center = frontHandWorld + (aimDirection * stringPullback);
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (ammoType <= ProjectileID.None)
            {
                return true;
            }

            Texture2D bowTexture = TextureAssets.Projectile[Projectile.type].Value;
            if (bowTexture == null)
            {
                return false;
            }

            int bowFrames = Math.Max(1, Main.projFrames[Projectile.type]);
            int bowFrameHeight = bowTexture.Height / bowFrames;
            int clampedFrame = Math.Clamp(Projectile.frame, 0, bowFrames - 1);
            Rectangle bowSourceRect = new Rectangle(0, clampedFrame * bowFrameHeight, bowTexture.Width, bowFrameHeight);
            float pivotY = (Projectile.height * 0.5f) - DrawOriginOffsetY;
            Vector2 bowOrigin = new Vector2(bowTexture.Width * 0.5f, pivotY);
            Vector2 drawWorld = Projectile.Center + new Vector2(0f, Projectile.gfxOffY);

            SpriteEffects spriteEffects = SpriteEffects.None;
            if (Projectile.spriteDirection == -1)
            {
                spriteEffects = SpriteEffects.FlipHorizontally;
            }

            Main.EntitySpriteDraw(
                bowTexture,
                drawWorld - Main.screenPosition,
                bowSourceRect,
                Projectile.GetAlpha(lightColor),
                Projectile.rotation,
                bowOrigin,
                Projectile.scale,
                spriteEffects,
                0);

            return false;
        }

        public override void PostDraw(Color lightColor)
        {
            DrawNockedArrow(lightColor);
            DrawPoints();
        }

        protected virtual void DrawNockedArrow(Color lightColor)
        {
            int arrowProjType = ammoType;
            if (arrowProjType <= ProjectileID.None || arrowProjType >= TextureAssets.Projectile.Length)
            {
                return;
            }

            Main.instance.LoadProjectile(arrowProjType);
            Texture2D arrowTexture = TextureAssets.Projectile[arrowProjType].Value;
            Texture2D bowTexture = TextureAssets.Projectile[Projectile.type].Value;
            if (arrowTexture == null || bowTexture == null)
            {
                return;
            }

            int arrowFrames = Math.Max(1, Main.projFrames[arrowProjType]);
            int arrowFrameHeight = arrowTexture.Height / arrowFrames;
            Rectangle arrowSourceRect = new Rectangle(0, 0, arrowTexture.Width, arrowFrameHeight);

            int bowFrames = Math.Max(1, Main.projFrames[Projectile.type]);
            int bowFrameHeight = bowTexture.Height / bowFrames;

            // All ChargedBowHeld sheets place the unpulled string 15px above the frame bottom on frame 0
            // and 1px above the frame bottom on the final frame. Convert that texture-space Y into a
            // scaled distance behind the bow's rotation pivot (Projectile.height / 2 - DrawOriginOffsetY).
            float pivotY = (Projectile.height * 0.5f) - DrawOriginOffsetY;
            float clampedCharge = MathHelper.Clamp(charge, 0f, 1f);
            float stringY = MathHelper.Lerp(bowFrameHeight - 15f, bowFrameHeight - 1f, clampedCharge);
            float stringPullback = (stringY - pivotY) * Projectile.scale;

            Vector2 aimDirection = Projectile.velocity.SafeNormalize(Vector2.UnitX * Projectile.direction);
            Vector2 stringNockWorld = Projectile.Center - (aimDirection * stringPullback) + new Vector2(0f, Projectile.gfxOffY);
            Vector2 arrowOrigin = new Vector2(arrowTexture.Width * 0.5f, arrowFrameHeight - 2f);

            SpriteEffects spriteEffects = SpriteEffects.None;
            if (Projectile.spriteDirection == -1)
            {
                spriteEffects = SpriteEffects.FlipHorizontally;
            }

            Main.EntitySpriteDraw(
                arrowTexture,
                stringNockWorld - Main.screenPosition,
                arrowSourceRect,
                Projectile.GetAlpha(lightColor),
                Projectile.rotation,
                arrowOrigin,
                1f,
                spriteEffects,
                0);
        }

        protected virtual void DrawPoints()
        {
            if (ModContent.GetInstance<tsorcRevampConfig>().ChargeCircleOpacity == 0) return;

            // Keep charged bows in PlayerDrawLayers.HeldProj (under PlayerDrawLayers.ArmOverItem) so the front hand draws over the arrow nock.
            if (ammoType <= ProjectileID.None && !Main.instance.DrawCacheProjsOverWiresUI.Contains(Projectile.whoAmI))
            {
                Main.instance.DrawCacheProjsOverWiresUI.Add(Projectile.whoAmI);
            }

            int maxPoints = 72;
            int points = (int)(charge * maxPoints) + 1;
            float opacity = (float)ModContent.GetInstance<tsorcRevampConfig>().ChargeCircleOpacity / 200;
            if (pointTexture == null || pointTexture.IsDisposed) pointTexture = ModContent.Request<Texture2D>("tsorcRevamp/Textures/ChargePoint", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            Rectangle srect = new(0, 0, pointTexture.Width, pointTexture.Height);
            Vector2 origin = new(2, 2);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, (Effect)null, Main.GameViewMatrix.TransformationMatrix);
            for (int i = 0; i < points - 1; i++)
            {
                Vector2 pos = (Main.MouseScreen + new Vector2(6, 6)) - (Vector2.UnitY * 24).RotatedBy(MathHelper.ToRadians((360 / maxPoints) * i));
                Main.EntitySpriteDraw(pointTexture, pos, srect, Color.White * opacity, 0f, origin, 1f, SpriteEffects.None, 0);
            }
            UsefulFunctions.RestartSpritebatch(ref Main.spriteBatch);

        }


        /// <summary>
        /// Get the value [amount]% of the way from [min] to [max]
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <param name="amount">A percentage (0-1)</param>
        /// <returns></returns>
        protected static float LerpFloat(float min, float max, float amount)
        {
            float diff = max - min;
            return min + diff * amount;
        }
    }
}
