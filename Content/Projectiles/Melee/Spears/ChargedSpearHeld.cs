using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Projectiles.Melee.Spears
{
    /// <summary>
    /// Shared held projectile for the tap-to-stab / hold-to-throw spears (Gae Bolg, Longinus, Dark Trident).
    /// The press puts the spear in a ready stance aimed at the cursor and starts the charge. Letting go within
    /// TapWindowTicks stabs; holding longer swings it up overhead (RaiseTicks) and letting go throws it, with
    /// damage and speed scaled by charge.
    /// </summary>
    public abstract class ChargedSpearHeld : ChargedBowHeld
    {
        // A release this many ticks or fewer after the press counts as a tap, so it stabs instead of throwing.
        // 12 ticks = 0.2s: long enough for a deliberate click, short enough that the stab doesn't feel late.
        public const int TapWindowTicks = 12;

        // Ticks to swing from the ready stance up to the raised throw pose once the tap window has passed.
        public const int RaiseTicks = 10;

        protected abstract int ThrownType { get; }
        protected abstract int PokeType { get; }

        // Pixels from the player's center to this sprite's center in the ready stance (during the tap window).
        // Matched to where the poke starts its thrust, so a tap extends straight out of the ready pose.
        protected abstract float ReadyHoldoutDistance { get; }

        // Armor-dye items whose shaders tint the charge overlay: below full charge, and at full charge.
        protected abstract int ChargingDyeItem { get; }
        protected abstract int FullChargeDyeItem { get; }

        protected abstract void PlayFullChargeCue(Player player);

        int ticksHeld = 0;

        // The item's stat-scaled damage, captured at spawn. The base AI zeroes Projectile.damage every tick
        // (the held spear itself must never hit), so it can't be read back at release.
        int stabDamage = 0;

        bool playedFullChargeCue = false;

        bool IsInTapWindow => ticksHeld <= TapWindowTicks;

        public override void OnSpawn(IEntitySource source)
        {
            stabDamage = Projectile.damage;
        }

        // Out of stamina while held: finish the attack as if released, instead of cancelling it. A click into
        // stamina debt hits 0 on the first tick, so that click still stabs.
        protected override void OnStaminaDepleted(Player player)
        {
            Shoot();
            Terraria.Audio.SoundEngine.PlaySound(soundtype, player.Center);
        }

        protected override void Shoot()
        {
            Player player = Main.player[Projectile.owner];
            if (player.whoAmI != Main.myPlayer)
            {
                return;
            }

            IEntitySource itemSource = player.GetSource_ItemUse(player.inventory[player.selectedItem]);

            if (IsInTapWindow)
            {
                // Tap: stab toward the cursor. The poke times itself to itemAnimationMax, so restart the use
                // animation at full length; the stab plays out completely and the item can't be re-used mid-stab.
                Vector2 stabDirection = (Main.MouseWorld - player.MountedCenter).SafeNormalize(Vector2.UnitX * player.direction);

                int facing = 1;
                if (stabDirection.X < 0)
                {
                    facing = -1;
                }
                player.ChangeDir(facing);

                player.HeldItem.useStyle = ItemUseStyleID.Shoot;
                player.itemRotation = (stabDirection * player.direction).ToRotation();
                player.itemAnimation = player.itemAnimationMax;
                player.itemTime = player.itemAnimationMax;

                // Raising itemAnimation from the held pin (2) back to full reads as a brand-new use to the stamina
                // system, which charged the click's cost a second time. Move its tracker up with it.
                player.GetModPlayer<tsorcRevampPlayer>().oldItemAnimation = player.itemAnimationMax;

                // Length 2: DarkTridentPoke moves by velocity * moveFactor and its draw offset assumes it.
                // ModdedSpearProjectile pokes normalize the velocity anyway.
                Vector2 pokeVelocity = stabDirection * 2f;
                Projectile.NewProjectile(itemSource, player.MountedCenter, pokeVelocity, PokeType, stabDamage, Projectile.knockBack, Projectile.owner);
                return;
            }

            float velocity = LerpFloat(minVelocity, maxVelocity, charge);
            Vector2 throwVelocity = UsefulFunctions.BallisticTrajectory(Projectile.Center, Main.MouseWorld, velocity, 0.1f, false, true);
            int damage = (int)LerpFloat(minDamage, maxDamage, charge);

            // ai[0] = 1 tells the thrown spear it was fully charged (explosion / ignite variants).
            float fullyCharged = 0;
            if (charge >= 1)
            {
                fullyCharged = 1;
            }

            Projectile.NewProjectile(itemSource, Projectile.Center, throwVelocity, ThrownType, damage, Projectile.knockBack, Projectile.owner, fullyCharged);
        }

        protected override void UpdateAim()
        {
            Projectile.timeLeft = 2;
            Player player = Main.player[Projectile.owner];

            ticksHeld++;

            // Aim direction lives in velocity (unit length) so other clients can follow it: the owner reads the
            // cursor and flags a sync when it changes; everyone else uses the synced value.
            Vector2 aimDirection = Projectile.velocity.SafeNormalize(Vector2.UnitX * player.direction);
            if (player.whoAmI == Main.myPlayer)
            {
                aimDirection = (Main.MouseWorld - player.MountedCenter).SafeNormalize(aimDirection);
                if (aimDirection != Projectile.velocity)
                {
                    Projectile.netUpdate = true;
                }
            }
            Projectile.velocity = aimDirection;

            int facing = 1;
            if (aimDirection.X < 0)
            {
                facing = -1;
            }
            Projectile.direction = facing;
            player.ChangeDir(facing);

            // 0 = ready stance (level at the cursor, where the stab starts), 1 = raised overhead to throw.
            // Held at 0 for the whole tap window so a tap never shows the raise; after that it eases to 1 over
            // RaiseTicks, while the charge keeps building underneath.
            float raiseProgress = 0f;
            int ticksPastTapWindow = ticksHeld - TapWindowTicks;
            if (ticksPastTapWindow > 0)
            {
                float linearProgress = MathHelper.Min(1f, ticksPastTapWindow / (float)RaiseTicks);
                raiseProgress = UsefulFunctions.EasingCurve(linearProgress);
            }

            // Spear points at the cursor the whole time; only its position travels, from in front of the hand to
            // overhead. Overhead it's pulled back 5-15px along the aim as the charge builds.
            Projectile.rotation = aimDirection.ToRotation() + MathHelper.PiOver2;
            Vector2 readyCenter = player.MountedCenter + aimDirection * ReadyHoldoutDistance;
            float pullBack = 5 + (10 * UsefulFunctions.EasingCurve(charge));
            Vector2 raisedCenter = player.Center + new Vector2(0, -15) - aimDirection * pullBack;
            Projectile.Center = Vector2.Lerp(readyCenter, raisedCenter, raiseProgress);

            // Front arm on a composite rotation, so it swings smoothly instead of snapping between arm frames.
            // Composite rotation 0 = arm hanging down, so world angle A is A - 90deg and straight up is -180deg.
            // AngleLerp takes the short way round, which lifts the arm upward on either facing. Vanilla clears
            // the composite arm at the start of every ItemCheck, so it has to be set again each tick.
            float readyArmRotation = aimDirection.ToRotation() - MathHelper.PiOver2;
            float raisedArmRotation = -MathHelper.Pi;
            float armRotation = Utils.AngleLerp(readyArmRotation, raisedArmRotation, raiseProgress);
            player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, armRotation);

            // Shoot useStyle picks the body/back-arm frame from itemRotation, so feed it the front arm's angle.
            Vector2 armDirection = (armRotation + MathHelper.PiOver2).ToRotationVector2();
            player.itemRotation = (armDirection * player.direction).ToRotation();
            player.heldProj = Projectile.whoAmI;
            player.itemAnimation = 2;
            player.itemTime = 2;

            if (charge >= 1 && !playedFullChargeCue)
            {
                PlayFullChargeCue(player);
                playedFullChargeCue = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            SpriteEffects spriteEffects = SpriteEffects.None;
            if (Projectile.spriteDirection == -1)
            {
                spriteEffects = SpriteEffects.FlipHorizontally;
            }

            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            int frameHeight = texture.Height / Main.projFrames[Projectile.type];
            int startY = frameHeight * Projectile.frame;
            Rectangle sourceRectangle = new Rectangle(0, startY, texture.Width, frameHeight);
            Vector2 origin = sourceRectangle.Size() / 2f;

            Main.EntitySpriteDraw(texture,
                Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY),
                sourceRectangle, lightColor, Projectile.rotation, origin, Projectile.scale, spriteEffects, 0);

            return false;
        }

        public override void PostDraw(Color lightColor)
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            int dyeItem = ChargingDyeItem;
            if (charge >= 1)
            {
                dyeItem = FullChargeDyeItem;
            }
            ArmorShaderData data = GameShaders.Armor.GetSecondaryShader((byte)GameShaders.Armor.GetShaderIdFromItemId(dyeItem), Main.LocalPlayer);
            data.Apply(null);

            SpriteEffects spriteEffects = SpriteEffects.None;
            if (Projectile.spriteDirection == -1)
            {
                spriteEffects = SpriteEffects.FlipHorizontally;
            }

            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            int frameHeight = texture.Height / Main.projFrames[Projectile.type];
            int startY = frameHeight * Projectile.frame;
            Rectangle sourceRectangle = new Rectangle(0, startY, texture.Width, frameHeight);
            Vector2 origin = sourceRectangle.Size() / 2f;

            // Overlay grows along the spear as it charges.
            Rectangle cropped = new Rectangle(sourceRectangle.X, sourceRectangle.Y, texture.Width, (int)(texture.Height * charge));

            Main.EntitySpriteDraw(texture,
                Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY),
                cropped, Color.White, Projectile.rotation, origin, Projectile.scale, spriteEffects, 0);

            UsefulFunctions.RestartSpritebatch(ref Main.spriteBatch);
            DrawPoints();
        }
    }
}
