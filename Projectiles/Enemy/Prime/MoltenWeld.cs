using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Graphics;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles.VFX;

namespace tsorcRevamp.Projectiles.Enemy.Prime
{
    class MoltenWeld : DynamicTrail
    {
        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
            // DisplayName.SetDefault("Poison Wave");
        }
        public override void SetDefaults()
        {
            Projectile.damage = 0;
            Projectile.width = 1;
            Projectile.height = 1;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 400;
            Projectile.penetrate = -1;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.hide = true;

            trailPointLimit = 150;
            trailMaxLength = 9999999;
            collisionPadding = 2;
            collisionFrequency = 2;
            trailWidth = 25;
            NPCSource = true;
            trailCollision = true;
            collisionFrequency = 2;
            noFadeOut = false;
            customEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/MoltenWeld", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
        }

        public override void AI()
        {
            trailPointLimit = 300;
            Projectile.timeLeft++;
            if (!initialized)
            {
                Initialize();
            }

            if ((!HostEntityValid() || Projectile.timeLeft < 1f / deathSpeed || dying) && !noFadeOut)
            {
                dying = true;
                hostNPC = null;

                deathProgress += deathSpeed;
                if (deathProgress > 1)
                {
                    deathProgress = 1;
                    Projectile.Kill();
                }
                fadeOut = 1f - deathProgress;
            }
            else
            {
                Projectile.Center = HostEntity.Center;

                //Don't add new trail segments if it has not travelled far enough
                if (Vector2.Distance(lastPosition, HostEntity.Center) > 1f)
                {
                    lastPosition = HostEntity.Center + new Vector2(0, 42);
                    trailPositions.Add(HostEntity.Center);
                    trailRotations.Add(HostEntity.velocity.ToRotation());
                }

                if (trailPositions.Count > 2)
                {
                    trailPositions[trailPositions.Count - 1] = HostEntity.Center + new Vector2(0, 42);
                    trailRotations[trailRotations.Count - 1] = HostEntity.velocity.ToRotation();

                    trailCurrentLength = CalculateLength();

                    if (trailCurrentLength > trailMaxLength)
                    {
                        float shorteningDistance = trailCurrentLength - trailMaxLength;

                        while (shorteningDistance > Vector2.Distance(trailPositions[0], trailPositions[1]))
                        {
                            trailPositions.RemoveAt(0);
                            trailRotations.RemoveAt(0);
                            trailCurrentLength = CalculateLength();
                            shorteningDistance = trailCurrentLength - trailMaxLength;
                        }
                        if (shorteningDistance < Vector2.Distance(trailPositions[0], trailPositions[1]))
                        {
                            Vector2 diff = trailPositions[1] - trailPositions[0];
                            float currentDistance = diff.Length();
                            float newDistance = currentDistance - shorteningDistance;
                            trailPositions[0] = trailPositions[1] - Vector2.Normalize(diff) * newDistance;
                            if (Vector2.Distance(trailPositions[0], trailPositions[1]) < 0.1f)
                            {
                                trailPositions.RemoveAt(0);
                                trailRotations.RemoveAt(0);
                                trailCurrentLength = CalculateLength();
                            }
                        }
                    }
                }

                //This could be optimized to not require recomputing the length after each removal
                while (trailPositions.Count > trailPointLimit)
                {
                    trailPositions.RemoveAt(0);
                    trailRotations.RemoveAt(0);
                    trailCurrentLength = CalculateLength();
                }
            }
        }

        public override float CollisionWidthFunction(float progress)
        {
            return 10;
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            behindNPCs.Add(index);
        }

        public override void SetEffectParameters(Effect effect)
        {
            collisionEndPadding = trailPositions.Count / 8;

            //Shifts its color slightly over time
            Vector3 hslColor = Main.rgbToHsl(Color.OrangeRed);
            hslColor.X += 0.03f * (float)Math.Cos(Main.timeForVisualEffects / 25f);
            Color rgbColor = Main.hslToRgb(hslColor);


            effect = customEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/MoltenWeld", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;

            effect.Parameters["effectColor"].SetValue(new Vector4(1.0f, 0.3f, 0.05f, 1));
            effect.Parameters["WorldViewProjection"].SetValue(GetWorldViewProjectionMatrix());
        }

    }
}