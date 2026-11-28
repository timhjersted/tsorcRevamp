using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Weapons.Summon.Whips.ModdedWhip;
using tsorcRevamp.Content.Projectiles;
using tsorcRevamp.NPCs;

namespace tsorcRevamp.Content.Items.Weapons.Summon.Whips.SerpentsTailLash
{
    public class SerpentsTailLashProj : ModdedWhipProjectile
    {
        public override int WhipWidth => 16;
        public override int WhipHeight => 16;
        public override int WhipSegments => 22;
        public override float WhipRangeMult => 1.32f;
        public override int DustId => DustID.Venom;
        public override int DustWidth => 10;
        public override int DustHeight => 10;
        public override Color DustColor => default;
        public override float DustScale => 1.0f;
        public override float MaxChargeTime => 0;
        public override Vector2 WhipTipBase => new Vector2(16, 20);
        public override float MaxChargeDmgMultiplier => 1f;
        public override float ChargeRangeBonus => 0;
        public override int WhipDebuffId => ModContent.BuffType<SerpentsTailLashDebuff>();
        public override int WhipDebuffDuration => DefaultWhipDebuffDuration;
        public override float WhipMultihitPenalty => 0.72f;
        public override Color WhipLineColor => new Color(148, 72, 224);

        private bool dropletsFired;

        // ai[2] packs displayed reinforcement level (>> 1) and needle-strike flag (& 1).
        private int PackedState => (int)Projectile.ai[2];
        private bool IsNeedleStrike => (PackedState & 1) == 1;
        private byte ReinforcementLevel => (byte)(PackedState >> 1);
        private bool HasTier3Perk => ReinforcementLevel >= SerpentsTailLashItem.PerkTier3Level;
        private bool HasTier6Perk => ReinforcementLevel >= SerpentsTailLashItem.PerkTier6Level;

        public override void OnSpawn(IEntitySource source)
        {
            base.OnSpawn(source);

            if (IsNeedleStrike)
            {
                if (HasTier3Perk)
                {
                    Projectile.WhipSettings.RangeMultiplier = 1.75f;
                    Projectile.WhipSettings.Segments = 28;
                }
                else
                {
                    // Needle strikes extend farther into a sharp thrust.
                    Projectile.WhipSettings.RangeMultiplier = 1.58f;
                    Projectile.WhipSettings.Segments = 26;
                }
            }
            else if (HasTier3Perk)
            {
                Projectile.WhipSettings.RangeMultiplier = 1.48f;
                Projectile.WhipSettings.Segments = 24;
            }
        }

        public override void CustomAIDustAndTipEffects(List<Vector2> points)
        {
            if (points.Count == 0)
            {
                return;
            }

            Vector2 tipPosition = points[points.Count - 1];

            if (!Main.dedServ)
            {
                if (IsNeedleStrike)
                {
                    Dust tipGlow = Dust.NewDustPerfect(tipPosition, DustID.AncientLight, Main.rand.NextVector2Circular(1.4f, 1.4f), 50, new Color(190, 90, 255), 1.15f);
                    tipGlow.noGravity = true;
                }
                else if (Main.rand.NextBool(2))
                {
                    Dust venomTip = Dust.NewDustPerfect(tipPosition, DustID.AncientLight, Main.rand.NextVector2Circular(0.8f, 0.8f), 60, new Color(155, 70, 225), 0.9f);
                    venomTip.noGravity = true;
                }
            }

            if (!IsNeedleStrike || dropletsFired)
            {
                return;
            }

            Player owner = Main.player[Projectile.owner];
            float swingTime = owner.itemAnimationMax * Projectile.MaxUpdates;
            if (Timer < swingTime * 0.48f)
            {
                return;
            }

            dropletsFired = true;

            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item17 with { Volume = 0.5f, Pitch = 0.3f }, tipPosition);

                for (int i = 0; i < 8; i++)
                {
                    Vector2 sparkVel = Main.rand.NextVector2Circular(3.2f, 3.2f);
                    Dust spark = Dust.NewDustPerfect(tipPosition, DustID.AncientLight, sparkVel, 50, new Color(200, 105, 255), 1.1f);
                    spark.noGravity = true;
                }
            }

            if (Main.myPlayer != Projectile.owner)
            {
                return;
            }

            Vector2 aimDir = Projectile.velocity.SafeNormalize(Vector2.UnitX * owner.direction);
            float dropletScale = HasTier6Perk ? SerpentsTailLashItem.Tier6NeedleDropletPercent / 100f : 0.6f;
            int dropletDamage = (int)(Projectile.damage * dropletScale);
            if (dropletDamage < 1)
            {
                dropletDamage = 1;
            }

            int dropletCount = HasTier6Perk ? SerpentsTailLashItem.Tier6NeedleDropletCount : SerpentsTailLashItem.NeedleDropletCount;
            float acidVenomFlag = HasTier3Perk ? 1f : 0f;

            // Flick short-range venom droplets forward from the needle tip at the apex of a Needle Strike.
            for (int i = 0; i < dropletCount; i++)
            {
                float fraction = dropletCount > 1 ? i / (float)(dropletCount - 1) : 0.5f;
                float spreadDegrees = MathHelper.Lerp(HasTier6Perk ? -14f : -10f, HasTier6Perk ? 14f : 10f, fraction);
                Vector2 dropletVel = aimDir.RotatedBy(MathHelper.ToRadians(spreadDegrees)) * 7.5f + new Vector2(0f, -1.6f);
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    tipPosition,
                    dropletVel,
                    ModContent.ProjectileType<SerpentVenomDropletProj>(),
                    dropletDamage,
                    1.5f,
                    Projectile.owner,
                    SerpentVenomDropletProj.SummonDamageTag,
                    acidVenomFlag);
            }
        }

        public override void CustomModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (IsNeedleStrike)
            {
                modifiers.SourceDamage *= 1f + SerpentsTailLashItem.NeedleBonusPercent / 100f;
            }
        }

        public override void CustomOnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.GetGlobalNPC<tsorcRevampGlobalNPC>().serpentsTailLashReinforcementLevel = ReinforcementLevel;

            if (HasTier3Perk || IsNeedleStrike)
            {
                target.AddBuff(BuffID.Venom, SerpentsTailLashItem.VenomDurationSeconds * 60);
            }
            else
            {
                target.AddBuff(BuffID.Poisoned, SerpentsTailLashItem.VenomDurationSeconds * 60);
            }

            if (IsNeedleStrike)
            {
                if (!Main.dedServ)
                {
                    for (int i = 0; i < 10; i++)
                    {
                        Vector2 burstVel = Main.rand.NextVector2Circular(3.8f, 3.8f);
                        Dust burst = Dust.NewDustPerfect(target.Center, DustID.Venom, burstVel, 45, default, 1.15f);
                        burst.noGravity = true;
                    }
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            List<Vector2> controlPoints = new List<Vector2>();
            Projectile.FillWhipControlPoints(Projectile, controlPoints);

            DrawLine(controlPoints);

            SpriteEffects flip = SpriteEffects.FlipHorizontally;
            if (Projectile.spriteDirection < 0)
            {
                flip = SpriteEffects.None;
            }

            Main.instance.LoadProjectile(Type);
            Texture2D texture = TextureAssets.Projectile[Type].Value;

            Vector2 drawPosition = controlPoints[0];

            for (int i = 0; i < controlPoints.Count - 1; i++)
            {
                Rectangle frame = new Rectangle(0, 0, 16, 16);
                Vector2 origin = new Vector2(8f, 4f);
                float segmentScale = 1f;
                bool isTip = i == controlPoints.Count - 2;

                if (isTip)
                {
                    frame.Y = 70;
                    frame.Height = 20;

                    Projectile.GetWhipSettings(Projectile, out float timeToFlyOut, out int _, out float _);
                    float progress = Timer / timeToFlyOut;
                    float pulse = Utils.GetLerpValue(0.1f, 0.7f, progress, true) * Utils.GetLerpValue(0.9f, 0.7f, progress, true);
                    float peakScale = 1.45f;
                    if (IsNeedleStrike)
                    {
                        peakScale = 1.75f;
                    }

                    segmentScale = MathHelper.Lerp(0.55f, peakScale, pulse);
                }
                else if (i > 16)
                {
                    frame.Y = 52;
                    frame.Height = 18;
                }
                else if (i > 8)
                {
                    frame.Y = 34;
                    frame.Height = 18;
                }
                else if (i > 0)
                {
                    frame.Y = 16;
                    frame.Height = 18;
                }

                Vector2 currentPoint = controlPoints[i];
                Vector2 segmentDelta = controlPoints[i + 1] - currentPoint;
                float rotation = segmentDelta.ToRotation() - MathHelper.PiOver2;
                Color drawColor = Lighting.GetColor(currentPoint.ToTileCoordinates());

                if (isTip && IsNeedleStrike)
                {
                    drawColor = Color.Lerp(drawColor, new Color(225, 165, 255), 0.65f);
                }

                Main.EntitySpriteDraw(
                    texture,
                    drawPosition - Main.screenPosition,
                    frame,
                    drawColor,
                    rotation,
                    origin,
                    segmentScale,
                    flip,
                    0);

                drawPosition += segmentDelta;
            }

            return false;
        }
    }
}
