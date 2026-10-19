using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Content.Projectiles.Enemy.Triad;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Content.Projectiles.Enemy.Archdeacon
{
    // The Archdeacon's Glintstone Snare: a glint lobbed at the floor near the player that plants a field of starlight.
    // Flight (ai[0] = 0): ballistic, Gravity px/t^2; the caster solves the launch to land on the player's floor point.
    //   Harmless in flight: the field is the threat.
    // Field (ai[0] = 1): FieldHalfWidth * 2 px wide, FieldHeight px tall, standing on the floor, for FieldTicks.
    //   A player who enters it is held (vanilla-style Hold debuff: no movement, attacking still works) for SnareHoldTicks,
    //   once per field, so they can walk out afterwards. Dodge-rolling across it is the counterplay: a rolling player
    //   is never snared.
    // Art: the HomingStarStar glint (intentional reuse) while flying; the field is dust + twinkling stars on the floor.
    // Death: a field fades over FadeOutTicks; a flight that never lands (fell into the void) fades in place.
    // MP: the field runs on every peer and each peer snares only its own player (Main.myPlayer); Hold is a normal
    // buff, synced by vanilla. Landing is detected on every peer, and the owner re-sends the settled position.
    public class ArchdeaconGlintSnare : ModProjectile
    {
        public const float Gravity = 0.2f;
        public const float FieldHalfWidth = 48f;
        public const int SnareHoldTicks = 120;
        private const float FieldHeight = 48f;
        private const int FieldTicks = 360;
        private const int FadeInTicks = 15;
        private const int FadeOutTicks = 30;
        private const int MaxFlightTicks = 120;
        private const float MaxFallSpeed = 12f;

        private bool IsField => Projectile.ai[0] == 1f;

        private int Age
        {
            get => (int)Projectile.localAI[0];
            set => Projectile.localAI[0] = value;
        }

        private int FieldAge
        {
            get => (int)Projectile.localAI[1];
            set => Projectile.localAI[1] = value;
        }

        // Only this client's own player is ever snared here, so one flag per field is enough.
        private bool _snaredLocalPlayer;
        private int _localHoldTicks;

        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(HomingStarStar));

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = MaxFlightTicks + FieldTicks;
            Projectile.DamageType = DamageClass.Magic;
        }

        // The snare never deals damage; it only holds.
        public override bool? CanDamage() => false;

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (!IsField)
            {
                Plant();
            }

            return false;
        }

        public override void AI()
        {
            Age++;

            if (IsField)
            {
                UpdateField();
                return;
            }

            Projectile.velocity.Y = Math.Min(Projectile.velocity.Y + Gravity, MaxFallSpeed);
            Projectile.rotation += 0.2f;
            Lighting.AddLight(Projectile.Center, ArchdeaconGlintStar.GlintColor.ToVector3() * 0.6f);

            if (!Main.dedServ)
            {
                Dust trail = Dust.NewDustPerfect(Projectile.Center, DustID.TintableDustLighted, -Projectile.velocity * 0.2f, 40,
                    ArchdeaconGlintStar.GlintColor, 1f);
                trail.noGravity = true;
            }

            bool fadingInAir = Age > MaxFlightTicks;
            if (fadingInAir)
            {
                Projectile.alpha = Math.Min(255, Projectile.alpha + 17);
                if (Projectile.alpha >= 255)
                {
                    Projectile.Kill();
                }
            }
        }

        // Landed: become a field standing on the surface it hit.
        private void Plant()
        {
            Projectile.ai[0] = 1f;
            Projectile.velocity = Vector2.Zero;
            Projectile.tileCollide = false;
            Projectile.timeLeft = FieldTicks;
            FieldAge = 0;

            Vector2 bottom = Projectile.Bottom;
            Projectile.Resize((int)(FieldHalfWidth * 2f), (int)FieldHeight);
            Projectile.Bottom = bottom;

            if (Projectile.owner == Main.myPlayer)
            {
                Projectile.netUpdate = true;
            }

            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.5f, Pitch = -0.3f }, Projectile.Center);

                for (int i = 0; i < 30; i++)
                {
                    Vector2 point = new Vector2(Projectile.Center.X + Main.rand.NextFloat(-FieldHalfWidth, FieldHalfWidth), Projectile.Bottom.Y - 2f);
                    Dust burst = Dust.NewDustPerfect(point, DustID.TintableDustLighted, new Vector2(0f, -Main.rand.NextFloat(1f, 3f)), 30,
                        ArchdeaconGlintStar.GlintCore, 1.1f);
                    burst.noGravity = true;
                }
            }
        }

        private void UpdateField()
        {
            // A client that learned of the landing from the owner's sync, not its own collision: the synced position is
            // already the field's top-left, so only the size needs adopting.
            int fieldWidth = (int)(FieldHalfWidth * 2f);
            if (Projectile.width != fieldWidth)
            {
                Projectile.width = fieldWidth;
                Projectile.height = (int)FieldHeight;
                Projectile.tileCollide = false;
                Projectile.timeLeft = FieldTicks;
            }

            FieldAge++;
            Projectile.velocity = Vector2.Zero;

            float opacity = MathHelper.Clamp(FieldAge / (float)FadeInTicks, 0f, 1f);
            if (Projectile.timeLeft < FadeOutTicks)
            {
                opacity = Projectile.timeLeft / (float)FadeOutTicks;
            }

            Projectile.alpha = (int)(255 * (1f - opacity));
            Lighting.AddLight(Projectile.Center, ArchdeaconGlintStar.GlintColor.ToVector3() * 0.5f * opacity);

            bool armed = FieldAge > FadeInTicks && Projectile.timeLeft > FadeOutTicks;
            if (!Main.dedServ)
            {
                TrySnareLocalPlayer(armed);
                FieldDust(opacity);
            }
        }

        private void TrySnareLocalPlayer(bool armed)
        {
            Player player = Main.LocalPlayer;
            if (!player.active || player.dead)
            {
                return;
            }

            // While held: a starlight tether from the field to the player shows why they can't move.
            if (_localHoldTicks > 0)
            {
                _localHoldTicks--;
                Vector2 alongTether = Vector2.Lerp(Projectile.Center, player.Center, Main.rand.NextFloat());
                Dust tether = Dust.NewDustPerfect(alongTether, DustID.TintableDustLighted, Vector2.Zero, 30, ArchdeaconGlintStar.GlintCore, 0.9f);
                tether.noGravity = true;
            }

            bool inside = player.Hitbox.Intersects(Projectile.Hitbox);
            bool rolling = player.GetModPlayer<tsorcRevampPlayer>().isDodging;
            if (!armed || _snaredLocalPlayer || !inside || rolling)
            {
                return;
            }

            _snaredLocalPlayer = true;
            _localHoldTicks = SnareHoldTicks;
            player.AddBuff(ModContent.BuffType<Hold>(), SnareHoldTicks);
            // The snare deals no damage, but it is still an Archdeacon attack: same debuffs as a hit.
            ArchdeaconGlintStar.ApplyHitDebuffs(player);
            SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.8f, Pitch = 0.3f }, player.Center);

            for (int i = 0; i < 24; i++)
            {
                Dust bind = Dust.NewDustPerfect(player.Center + Main.rand.NextVector2CircularEdge(24f, 32f), DustID.TintableDustLighted,
                    Vector2.Zero, 20, ArchdeaconGlintStar.GlintCore, 1.2f);
                bind.noGravity = true;
            }
        }

        // Starlight rising across the whole field: its dust spans exactly the snare area.
        private void FieldDust(float opacity)
        {
            if (Main.rand.NextFloat() > opacity)
            {
                return;
            }

            Vector2 point = new Vector2(Projectile.Center.X + Main.rand.NextFloat(-FieldHalfWidth, FieldHalfWidth), Projectile.Bottom.Y - 2f);
            Dust rise = Dust.NewDustPerfect(point, DustID.TintableDustLighted, new Vector2(0f, -Main.rand.NextFloat(0.6f, 1.6f)), 60,
                ArchdeaconGlintStar.GlintColor, 0.9f);
            rise.noGravity = true;
        }

        // Default look: in flight a small comet glint (ArchdeaconGlintComet); planted, the ArchdeaconGlintSnare field.
        // The field's ellipse is 80% of its quad's width, so the quad is drawn 1.25x the snare width to match the area.
        // It flashes bright for the first 20t after it snares this client's player.
        private void DrawShader()
        {
            float opacity = 1f - Projectile.alpha / 255f;

            if (!IsField)
            {
                float rotation = Projectile.velocity.SafeNormalize(Vector2.UnitY).ToRotation();
                EnemyVFX.DrawArchdeaconGlintComet(Projectile.Center, rotation, 20f, 0.4f, opacity);
                return;
            }

            float pulse = (_localHoldTicks - (SnareHoldTicks - 20)) / 20f;
            float quadWidth = FieldHalfWidth * 2f / 0.8f;
            EnemyVFX.DrawArchdeaconGlintSnare(new Vector2(Projectile.Center.X, Projectile.Bottom.Y), quadWidth, opacity, pulse);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (!GlintLegacyCommand.UseLegacySprites)
            {
                DrawShader();
                return false;
            }

            Texture2D star = TextureAssets.Projectile[Type].Value;
            float opacity = 1f - Projectile.alpha / 255f;
            Color glow = new Color(ArchdeaconGlintStar.GlintCore.R, ArchdeaconGlintStar.GlintCore.G, ArchdeaconGlintStar.GlintCore.B, 0) * opacity;

            if (!IsField)
            {
                Main.EntitySpriteDraw(star, Projectile.Center - Main.screenPosition, null, glow, Projectile.rotation, star.Size() * 0.5f, 0.9f,
                    SpriteEffects.None, 0);
                return false;
            }

            // Five twinkling stars set into the floor across the field's width.
            for (int i = 0; i < 5; i++)
            {
                float x = MathHelper.Lerp(-FieldHalfWidth + 8f, FieldHalfWidth - 8f, i / 4f);
                Vector2 point = new Vector2(Projectile.Center.X + x, Projectile.Bottom.Y - 6f) - Main.screenPosition;
                float twinkle = 0.45f + 0.15f * (float)Math.Sin(FieldAge * 0.15f + i * 1.3f);
                Main.EntitySpriteDraw(star, point, null, glow, FieldAge * 0.03f + i, star.Size() * 0.5f, twinkle, SpriteEffects.None, 0);
            }

            return false;
        }
    }
}
