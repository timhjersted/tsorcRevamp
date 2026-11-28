using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
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

        // Full-draw reward for bows that opt in (HasFullDrawPerk): a shot released at full charge deals this many
        // percent more damage and its arrows pierce this many extra enemies. Public so the bows' tooltips can show them.
        public const int FullDrawDamageBonusPercent = 20;
        public const int FullDrawBonusPierce = 1;

        // Opt-in per bow. Off by default so spears (which also derive from this class) and Cernos Prime are untouched.
        protected virtual bool HasFullDrawPerk => false;

        // Quick-shot floor for bows that opt in (HasQuickShot): an early release still flies at least this percent of
        // the bow's shootSpeed and deals at least this percent of its full damage, so a tap is a usable weak shot.
        // Without it a tap leaves at 10% speed (~2 px/tick for Artemis) and drops at the player's feet.
        public const int QuickShotDamagePercent = 20;
        public const int QuickShotVelocityPercent = 55;

        // A tap pays only this percent of the draw's stamina price; the rest is refunded on release, shrinking
        // linearly to no refund at full draw. Running stamina dry mid-draw refunds nothing.
        public const int QuickShotStaminaCostPercent = 40;

        // What this draw's use actually cost (read from the stamina player's snapshot), and whether it's been read yet.
        float drawStaminaPaid = 0f;
        bool readDrawStaminaPaid = false;

        // Default damage floor for bows without the quick shot (Cernos Prime): 10% on a tap. Their speed floor stays in SetStats.
        const int DefaultMinChargePercent = 10;

        // Arrow spread in degrees, narrowing linearly to FullDrawSpreadDegrees at full charge.
        // Quick-shot bows start much tighter than the 18 degree default so a tap actually goes where it's aimed.
        const float DefaultTapSpreadDegrees = 18f;
        const float QuickShotTapSpreadDegrees = 8f;
        const float FullDrawSpreadDegrees = 3f;

        // Opt-in per bow, same reasoning as HasFullDrawPerk.
        protected virtual bool HasQuickShot => false;

        bool playedFullDrawCue = false;

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

        // Add the arrow's own damage back onto the charged shot. Projectile.damage arrives from the item's Shoot as
        // weapon + ammo damage (all modifiers applied), and AI() zeroes it every tick, so it has to be read here.
        // The ammo share is that figure minus the bare weapon damage. Runs only on the owner, the only place Shoot() runs.
        // ChargedSpearHeld overrides OnSpawn without calling this, because spears have no ammo.
        public override void OnSpawn(IEntitySource source)
        {
            Player owner = Main.player[Projectile.owner];
            int weaponOnlyDamage = owner.GetWeaponDamage(owner.HeldItem);
            int ammoDamage = Math.Max(0, Projectile.damage - weaponOnlyDamage);

            maxDamage += ammoDamage;

            // Tap floors: quick-shot bows get the stronger floor on both damage and speed.
            // maxVelocity is the bow's shootSpeed, set in SetStats.
            if (HasQuickShot)
            {
                minDamage = maxDamage * QuickShotDamagePercent / 100;
                minVelocity = maxVelocity * QuickShotVelocityPercent / 100f;
            }
            else
            {
                minDamage = maxDamage * DefaultMinChargePercent / 100;
            }
        }

        // Aimed, charge-scaled launch velocity for one arrow: speed lerps min->max with charge and the random spread
        // narrows from the tap spread to FullDrawSpreadDegrees. Call per arrow so multi-arrow bows get separate spreads.
        protected Vector2 GetChargedArrowVelocity()
        {
            float speed = LerpFloat(minVelocity, maxVelocity, charge);

            float tapSpreadDegrees = DefaultTapSpreadDegrees;
            if (HasQuickShot)
            {
                tapSpreadDegrees = QuickShotTapSpreadDegrees;
            }

            float spreadDegrees = LerpFloat(tapSpreadDegrees, FullDrawSpreadDegrees, charge);
            Vector2 launchVelocity = (aimVector * speed).RotatedByRandom(MathHelper.ToRadians(spreadDegrees));
            return launchVelocity;
        }

        // Spawns one arrow, applying the full-draw reward when this bow has it and the shot is fully charged.
        // Damage is passed into NewProjectile so it syncs; the extra pierce only matters on the owner, which resolves the hits.
        protected void SpawnChargedArrow(Player player, int arrowType, Vector2 arrowVelocity, int damage)
        {
            bool fullDraw = HasFullDrawPerk && charge >= 1f;

            int arrowDamage = damage;
            if (fullDraw)
            {
                arrowDamage = damage * (100 + FullDrawDamageBonusPercent) / 100;
            }

            IEntitySource itemSource = player.GetSource_ItemUse(player.inventory[player.selectedItem]);
            int arrowIndex = Projectile.NewProjectile(itemSource, player.Center, arrowVelocity, arrowType, arrowDamage, Projectile.knockBack, Projectile.owner);

            // -1 is infinite pierce already; leave it alone.
            Projectile arrow = Main.projectile[arrowIndex];
            if (fullDraw && arrow.penetrate > 0)
            {
                arrow.penetrate += FullDrawBonusPierce;
            }
        }

        protected virtual void SpecialBehavior()
        {
        }

        protected virtual void Animate()
        {
            Projectile.frame = (int)((Main.projFrames[Projectile.type] - 1) * charge);

        }

        protected abstract void Shoot();

        // Runs once when stamina hits 0 while held, just before the projectile is removed. Bows loose the arrow at
        // whatever charge they reached instead of losing the draw. A click that overdraws into stamina debt lands
        // at 0 on the very first tick, so it fires a near-zero-charge shot. Spears override this with their stab/throw.
        protected virtual void OnStaminaDepleted(Player player)
        {
            Shoot();
            SoundEngine.PlaySound(soundtype, player.Center);
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

            tsorcRevampStaminaPlayer staminaPlayer = player.GetModPlayer<tsorcRevampStaminaPlayer>();

            // The draw's price is charged in PreItemCheck the tick after this projectile spawns (that's when
            // itemAnimation is seen rising), and players update before projectiles, so read it on that tick.
            // Stamina is owner-local state, so only the owner tracks it.
            if (HasQuickShot && !readDrawStaminaPaid && Projectile.owner == Main.myPlayer && staminaPlayer.staminaUseChargeTick == Main.GameUpdateCount)
            {
                drawStaminaPaid = Math.Max(0f, staminaPlayer.staminaBeforeUseCharge - staminaPlayer.staminaResourceCurrent);
                readDrawStaminaPaid = true;
            }

            float stamina = staminaPlayer.staminaResourceCurrent;
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

                    // Snap to exactly 1. Summing 1/useTime in floats can land at 0.99999 after useTime ticks,
                    // which would cost an extra tick before "charge >= 1" checks (full-draw perk, spear throws) pass.
                    if (charge > 0.999f)
                    {
                        charge = 1f;
                    }
                }

                // One-time "fully drawn" cue: a chime and a ring of golden sparks at the bow. Cosmetic only.
                if (HasFullDrawPerk && charge >= 1f && !playedFullDrawCue)
                {
                    playedFullDrawCue = true;

                    if (!Main.dedServ)
                    {
                        SoundEngine.PlaySound(SoundID.MaxMana with { Pitch = 0.3f, Volume = 0.8f }, player.Center);

                        // 14 sparks evenly spaced on a ring, flung outward at 2.5 px/tick.
                        for (int i = 0; i < 14; i++)
                        {
                            Vector2 sparkDirection = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * i / 14f);
                            Dust spark = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame, sparkDirection * 2.5f, 0, default, 1.1f);
                            spark.noGravity = true;
                        }
                    }
                }
            }
            else
            {
                Shoot();
                SoundEngine.PlaySound(soundtype, player.Center);
                fired = true;

                // Quick-shot refund: a tap gets back (100 - QuickShotStaminaCostPercent)% of the draw price, scaling
                // to nothing at full charge. drawStaminaPaid is 0 if the price was never read, so that refunds nothing.
                if (HasQuickShot && Projectile.owner == Main.myPlayer)
                {
                    float refundFraction = (100 - QuickShotStaminaCostPercent) / 100f * (1f - charge);
                    float refund = drawStaminaPaid * refundFraction;
                    staminaPlayer.staminaResourceCurrent = Math.Min(staminaPlayer.staminaResourceCurrent + refund, staminaPlayer.staminaResourceMax2);
                }
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
