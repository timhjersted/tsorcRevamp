using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using tsorcRevamp.Items;
using tsorcRevamp.Items.Weapons.Melee.Broadswords.BroadswordRework.Common.Melee;

namespace tsorcRevamp.Projectiles.VFX
{
    public class Slash : DynamicTrail
    {
        public override string Texture => base.Texture;
        public sealed override void SetDefaults()
        {
            Projectile.width = 1;
            Projectile.height = 1;
            Projectile.tileCollide = false;
            Projectile.friendly = false;
            Projectile.penetrate = -1;

            trailWidth = 45;
            trailPointLimit = 900;
            trailMaxLength = 500;
            Projectile.hide = true;
            collisionPadding = 5;
            NPCSource = false;
            trailCollision = false;
            noFadeOut = true;
            ScreenSpace = true;
            newPointDistance = 0.000f;
            customEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/Slash", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
        }
        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            behindNPCs.Add(index);
        }

        bool rotatingRight;
        bool initializedSlash;
        float speed;
        float progress;
        float lastPercent;
        Color slashColor = Color.White;
        float timeMax = 1;
        tsorcSlashStyle slashStyle = tsorcSlashStyle.Metal;
        bool flippedSwing = false;
        int AttackId = 0;

        float rotationDirection = 0;
        bool reachedEnd = false;
        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];

            if (!initializedSlash)
            {
                if (owner.HeldItem.TryGetGlobalItem(out ItemMeleeAttackAiming aimingInit))
                {
                    AttackId = aimingInit.AttackId;
                }
                flippedSwing = AttackId % 2 != 0;
                trailWidth = (int)(Math.Sqrt(owner.HeldItem.height * owner.HeldItem.height + owner.HeldItem.width * owner.HeldItem.width) * owner.HeldItem.scale);
                trailWidth = Math.Max(trailWidth, 50);
                Projectile.timeLeft = owner.itemAnimationMax + 10;
                tsorcInstancedGlobalItem instancedGlobal = owner.HeldItem.GetGlobalItem<tsorcInstancedGlobalItem>();
                slashColor = instancedGlobal.slashColor;
                slashStyle = instancedGlobal.slashStyle;
                initializedSlash = true;
            }

            Projectile.Center = owner.Center;

            if (!initialized)
            {
                Initialize();
            }


            if (owner.HeldItem.TryGetGlobalItem(out ItemMeleeAttackAiming aiming))
            {
                //Main.NewText(aiming.AttackId);
                if (AttackId != aiming.AttackId)
                {
                    reachedEnd = true;
                }
            }

            bool flipped = owner.gravDir == -1f && ModContent.GetInstance<tsorcRevampConfig>().GravityFix;

            if (Projectile.timeLeft > 10 && !reachedEnd)
            {
                Projectile.rotation = QuickSlashMeleeAnimation.MeleeSwingRotation(owner, owner.HeldItem, flippedSwing, 1.2f);
                if (owner.gravDir == 1f) Projectile.rotation += MathHelper.PiOver2;
                else if (owner.direction == 1) Projectile.rotation += (float)Math.PI;

                //Skip the first
                if (lastPercent == 0)
                {
                    lastPercent = Projectile.rotation;
                    return;
                }

                float subdivisionCount = (int)(Math.Abs(Projectile.rotation - lastPercent) * 120);
                for (float i = 0; i < subdivisionCount; i++)
                {
                    trailPositions.Add(owner.Center + new Vector2(trailWidth, 0).RotatedBy(MathHelper.Lerp(lastPercent, Projectile.rotation, i / subdivisionCount) - MathHelper.PiOver2) - Main.screenPosition);
                    trailRotations.Add(MathHelper.Lerp(lastPercent, Projectile.rotation, i / subdivisionCount) + MathHelper.Pi);
                }
            }
            lastPercent = Projectile.rotation;
        }

        //Projectile.rotation += speed * 3 * ((float)Math.Pow(Math.Sin(MathHelper.Pi * ((float)Projectile.timeLeft) / ((float)owner.HeldItem.useTime)), 5) + 0.08f);

        //Sample one line of the base noise texture, centered on this U coordinate
        float baseNoiseUOffset = 0;
        float trailIntensity = 1;
        public override void SetEffectParameters(Effect effect)
        {

            customEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/Slash", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;

            if (baseNoiseUOffset == 0)
            {
                baseNoiseUOffset = Main.rand.NextFloat();
            }

            Texture2D noiseTexture = tsorcRevamp.NoiseWavy;
            switch (slashStyle)
            {
                case tsorcSlashStyle.Metal:
                    {
                        noiseTexture = tsorcRevamp.NoiseVoronoi;
                        break;
                    }
                case tsorcSlashStyle.LightMagic:
                    {
                        noiseTexture = tsorcRevamp.NoiseVoronoi;
                        break;
                    }
                case tsorcSlashStyle.DarkMagic:
                    {
                        noiseTexture = tsorcRevamp.NoiseWavy;
                        break;
                    }
                case tsorcSlashStyle.Scifi:
                    {
                        noiseTexture = tsorcRevamp.NoiseCircuit;
                        break;
                    }
            }


            effect.Parameters["baseNoise"].SetValue(tsorcRevamp.NoiseSmooth);
            effect.Parameters["baseNoiseUOffset"].SetValue(baseNoiseUOffset);
            effect.Parameters["secondaryNoise"].SetValue(noiseTexture);

            visualizeTrail = false;
            if (Projectile.timeLeft < 15)
            {
                trailIntensity = Projectile.timeLeft / 15f;
            }

            effect.Parameters["fadeOut"].SetValue(trailIntensity);
            effect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly);
            effect.Parameters["slashCenter"].SetValue(Color.White.ToVector4());
            effect.Parameters["slashEdge"].SetValue(slashColor.ToVector4());
            effect.Parameters["WorldViewProjection"].SetValue(GetWorldViewProjectionMatrix());
        }


        public static Texture2D texture;
        public static Texture2D glowTexture;
        public override bool PreDraw(ref Color lightColor)
        {
            visualizeTrail = false;
            base.PreDraw(ref lightColor);
            return false;
        }
    }
}