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
            deathSpeed = 1f / 600f;
            customEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/MoltenWeld", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
        }

        int lifeTimer;
        public override void AI()
        {
            lifeTimer++;
            if (hostNPC != null && Projectile.ai[0] == 1)
            {
                if(lifeTimer == 0)
                {
                    hostNPC = null;
                }
            }

            hostOffset = new Vector2(0, 72);
            trailPointLimit = 300;
            Projectile.timeLeft++;
            if (!initialized)
            {
                lifeTimer = -60;
                Initialize();
            }
            noFadeOut = false;

            if ((!HostEntityValid() || dying) && !noFadeOut)
            {
                dying = true;
                hostNPC = null;
                if (lifeTimer > 600)
                {
                    deathProgress = 1;
                    Projectile.Kill();
                }
                if(lifeTimer > 540)
                {
                    fadeOut = 1f - ((lifeTimer - 540f) / 60f);
                    trailCollision = false;
                }
            }
            else
            {
                Projectile.Center = HostEntity.Center;

                //Don't add new trail segments if it has not travelled far enough
                if (Vector2.Distance(lastPosition, HostEntity.Center + hostOffset) > 1f)
                {
                    lastPosition = HostEntity.Center + hostOffset;
                    trailPositions.Add(HostEntity.Center + new Vector2(0, 72));
                    trailRotations.Add(HostEntity.velocity.ToRotation());
                }

                if (trailPositions.Count > 2)
                {
                    trailPositions[trailPositions.Count - 1] = HostEntity.Center + hostOffset;
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
            if (Projectile.ai[0] == 1)
            {
                effect.Parameters["start"].SetValue(0.2f);
                effect.Parameters["end"].SetValue(0.8f);
            }
            effect.Parameters["effectColor"].SetValue(new Vector4(1.0f, 0.3f, 0.05f, 1) * (float)Math.Pow(fadeOut, 0.3f));
            effect.Parameters["WorldViewProjection"].SetValue(GetWorldViewProjectionMatrix());
        }

    }
}