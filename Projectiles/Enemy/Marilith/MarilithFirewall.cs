using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;
using tsorcRevamp.NPCs.Bosses.SuperHardMode.Fiends;

namespace tsorcRevamp.Projectiles.Enemy.Marilith
{
    public class MarilithFirewall : ModProjectile
    {
        // ── WIP: Rounded wall controller ─────────────────────────────────────────────
        // Currently NOT spawned. To switch to the rounded wall system:
        //   1. In FireFiendMarilith.InitializeFirewalls() uncomment the single-piece spawn
        //      and comment out the 4-piece spawn.
        //   2. This class handles ai[0] == RoundedControllerStyle as its rounded path.
        public const int RoundedControllerStyle = 4;

        private const float WallThickness = 140f;
        private const float CornerRadius = 30f * 16f;
        private const float VisualPadding = 240f;
        private const int CornerSegments = 12;

        private Vector2 arenaCenter;
        private Vector2 arenaSize;
        private Vector2[] boundaryPoints;
        private Vector2 cachedBoundaryCenter;
        private Vector2 cachedBoundarySize;

        public static Effect RoundedFirewallEffect;
        // ─────────────────────────────────────────────────────────────────────────────

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 5;
        }

        public override void SetDefaults()
        {
            Projectile.width = 1;
            Projectile.height = 1;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.penetrate = 50;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 20;
        }

        int progress = 0;
        float cloudProgress = 0;

        public override void AI()
        {
            // Keep it alive indefinitely while Marilith is alive.
            Projectile.timeLeft = 2;
            if (!NPC.AnyNPCs(ModContent.NPCType<FireFiendMarilith>()))
            {
                Projectile.Kill();
                return;
            }

            // ── WIP: Rounded wall path (ai[0] == 4) ──────────────────────────────────
            if ((int)Projectile.ai[0] == RoundedControllerStyle)
            {
                FireFiendMarilith marilith = Main.npc[UsefulFunctions.GetFirstNPC(ModContent.NPCType<FireFiendMarilith>()).Value].ModNPC as FireFiendMarilith;
                if (marilith.MoveIndex == 1 && marilith.MoveTimer < 1800)
                {
                    cloudProgress = Math.Min(cloudProgress + 1, 300);
                }
                else
                {
                    cloudProgress = Math.Max(cloudProgress - 1, 0);
                }

                progress = Math.Min(progress + 1, 100);
                ConfigureArenaBounds();
                CastBoundaryLight();

                Projectile.frameCounter++;
                if (Projectile.frameCounter > 3)
                {
                    Projectile.frame = (Projectile.frame + 1) % 5;
                    Projectile.frameCounter = 0;
                }

                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    Player player = Main.player[i];
                    if (player.active && !player.dead && IntersectsRoundedWall(player.Hitbox))
                    {
                        player.statLife -= 5;
                        CombatText.NewText(player.Hitbox, Color.Red, 5);
                        if (player.statLife < 1)
                        {
                            player.statLife = 1;
                            player.immune = false;
                            player.immuneTime = 0;
                        }
                    }
                }
                return;
            }
            // ─────────────────────────────────────────────────────────────────────────

            // ── Original 4-piece wall logic (ai[0] == 0/1/2/3) ────────────────────────
            FireFiendMarilith marl = Main.npc[UsefulFunctions.GetFirstNPC(ModContent.NPCType<FireFiendMarilith>()).Value].ModNPC as FireFiendMarilith;
            if (Projectile.ai[0] == 2 && marl.MoveIndex == 1 && marl.MoveTimer < 1800)
            {
                cloudProgress++;
                if (cloudProgress > 300)
                    cloudProgress = 300;
            }
            else
            {
                cloudProgress--;
                if (cloudProgress < 0)
                    cloudProgress = 0;
            }

            if (progress < 100)
                progress++;

            Projectile.alpha = (int)(progress * 2.5f);

            if (ModContent.GetInstance<tsorcRevampConfig>().AdventureMode)
            {
                int width = 140;
                int longLength = 268;
                int shortLength = 122;

                if (tsorcRevampWorld.RemixMap)
                {
                    // Left
                    if (Projectile.ai[0] == 0)
                    {
                        Projectile.width = width;
                        Projectile.height = 16 * shortLength;
                        Projectile.Center = new Vector2(176, 1873) * 16;
                    }
                    // Right
                    else if (Projectile.ai[0] == 1)
                    {
                        Projectile.width = width;
                        Projectile.height = 16 * shortLength;
                        Projectile.Center = new Vector2(419.2f, 1873) * 16;
                    }
                    // Top
                    else if (Projectile.ai[0] == 2)
                    {
                        Projectile.width = 16 * longLength;
                        Projectile.height = width;
                        Projectile.Center = new Vector2(297.5f, 1824.3f) * 16;
                    }
                    // Bottom
                    else if (Projectile.ai[0] == 3)
                    {
                        Projectile.width = 16 * longLength;
                        Projectile.height = width;
                        Projectile.Center = new Vector2(297.5f, 1921.8f) * 16;
                    }
                }
                else
                {
                    // Adventure arena coords are legacy 2000-space → MapWorld (remix branch above is remix-native).
                    // The whole arena (Y 1682-1780) is in the flat +200 band. width/height are SIZES, not coords.
                    // Left
                    if (Projectile.ai[0] == 0)
                    {
                        Projectile.width = width;
                        Projectile.height = 16 * shortLength;
                        Projectile.Center = ExpandedWorldTransform.MapWorld(new Vector2(3107, 1731) * 16);
                    }
                    // Right
                    else if (Projectile.ai[0] == 1)
                    {
                        Projectile.width = width;
                        Projectile.height = 16 * shortLength;
                        Projectile.Center = ExpandedWorldTransform.MapWorld(new Vector2(3350.2f, 1731) * 16);
                    }
                    // Top
                    else if (Projectile.ai[0] == 2)
                    {
                        Projectile.width = 16 * longLength;
                        Projectile.height = width;
                        Projectile.Center = ExpandedWorldTransform.MapWorld(new Vector2(3228.5f, 1682.3f) * 16);
                    }
                    // Bottom
                    else if (Projectile.ai[0] == 3)
                    {
                        Projectile.width = 16 * longLength;
                        Projectile.height = width;
                        Projectile.Center = ExpandedWorldTransform.MapWorld(new Vector2(3228.5f, 1779.8f) * 16);
                    }
                }
            }

            // Original per-piece lighting: a straight line along the wall edge.
            DelegateMethods.v3_1 = Color.OrangeRed.ToVector3() * 2f;
            Vector2 startPoint = Projectile.Center;
            Vector2 endpoint = Projectile.Center;
            if (Projectile.ai[0] == 0)
            {
                startPoint.Y -= Projectile.height / 2;
                endpoint.Y += Projectile.height;
            }
            else
            {
                startPoint.X -= Projectile.width / 2;
                endpoint.X += Projectile.width / 2;
            }
            Utils.PlotTileLine(startPoint, endpoint, 16, DelegateMethods.CastLight);

            Projectile.frameCounter++;
            if (Projectile.frameCounter > 3)
            {
                Projectile.frame++;
                Projectile.frameCounter = 0;
            }
            if (Projectile.frame >= 5)
                Projectile.frame = 0;

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                if (Projectile.Hitbox.Contains(Main.player[i].Center.ToPoint()))
                {
                    Main.player[i].statLife -= 5;
                    CombatText.NewText(Main.player[i].Hitbox, Color.Red, 5);
                    if (Main.player[i].statLife < 1)
                    {
                        Main.player[i].statLife = 1;
                        Main.player[i].immune = false;
                        Main.player[i].immuneTime = 0;
                    }
                }
            }
            // ─────────────────────────────────────────────────────────────────────────
        }

        public override void ModifyHitPlayer(Player target, ref Player.HurtModifiers modifiers)
        {
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            if (progress >= 100)
            {
                //target.immune = false;
                //target.immuneTime = 0;
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            // Rounded wall uses custom polygon collision; old 4-piece uses default AABB.
            if ((int)Projectile.ai[0] == RoundedControllerStyle)
                return IntersectsRoundedWall(targetHitbox);
            return null;
        }


        // ── WIP: Rounded wall helper methods ─────────────────────────────────────────

        private void ConfigureArenaBounds()
        {
            if (ModContent.GetInstance<tsorcRevampConfig>().AdventureMode)
            {
                Vector2 topLeftBoundary;
                Vector2 bottomRightBoundary;
                if (tsorcRevampWorld.RemixMap)
                {
                    topLeftBoundary = new Vector2(176f, 1824.3f) * 16f;
                    bottomRightBoundary = new Vector2(419.2f, 1921.8f) * 16f;
                }
                else
                {
                    topLeftBoundary = ExpandedWorldTransform.MapWorld(new Vector2(3107f, 1682.3f) * 16f);
                    bottomRightBoundary = ExpandedWorldTransform.MapWorld(new Vector2(3350.2f, 1779.8f) * 16f);
                }

                arenaCenter = (topLeftBoundary + bottomRightBoundary) * 0.5f;
                arenaSize = bottomRightBoundary - topLeftBoundary;
            }
            else if (arenaSize == Vector2.Zero)
            {
                // Preserve a functional free-play fallback centered on the point at which the wall spawned.
                arenaCenter = Projectile.Center;
                arenaSize = new Vector2(2000f, 1600f);
            }

            Projectile.width = (int)Math.Ceiling(arenaSize.X + VisualPadding * 2f);
            Projectile.height = (int)Math.Ceiling(arenaSize.Y + VisualPadding * 2f);
            Projectile.Center = arenaCenter;

            if (boundaryPoints == null || cachedBoundaryCenter != arenaCenter || cachedBoundarySize != arenaSize)
            {
                BuildBoundaryPoints();
                cachedBoundaryCenter = arenaCenter;
                cachedBoundarySize = arenaSize;
            }
        }

        private void BuildBoundaryPoints()
        {
            float radius = Math.Min(CornerRadius, Math.Min(arenaSize.X, arenaSize.Y) * 0.5f);
            float left = arenaCenter.X - arenaSize.X * 0.5f;
            float right = arenaCenter.X + arenaSize.X * 0.5f;
            float top = arenaCenter.Y - arenaSize.Y * 0.5f;
            float bottom = arenaCenter.Y + arenaSize.Y * 0.5f;

            List<Vector2> points = new List<Vector2>(CornerSegments * 4 + 5)
            {
                new Vector2(left + radius, top),
                new Vector2(right - radius, top)
            };

            AppendArc(points, new Vector2(right - radius, top + radius), -MathHelper.PiOver2, 0f, radius);
            points.Add(new Vector2(right, bottom - radius));
            AppendArc(points, new Vector2(right - radius, bottom - radius), 0f, MathHelper.PiOver2, radius);
            points.Add(new Vector2(left + radius, bottom));
            AppendArc(points, new Vector2(left + radius, bottom - radius), MathHelper.PiOver2, MathHelper.Pi, radius);
            points.Add(new Vector2(left, top + radius));
            AppendArc(points, new Vector2(left + radius, top + radius), MathHelper.Pi, MathHelper.Pi * 1.5f, radius);

            boundaryPoints = points.ToArray();
        }

        private static void AppendArc(List<Vector2> points, Vector2 center, float startAngle, float endAngle, float radius)
        {
            for (int i = 1; i <= CornerSegments; i++)
            {
                float angle = MathHelper.Lerp(startAngle, endAngle, i / (float)CornerSegments);
                points.Add(center + angle.ToRotationVector2() * radius);
            }
        }

        private bool IntersectsRoundedWall(Rectangle targetHitbox)
        {
            if (boundaryPoints == null)
                return false;

            float collisionPoint = 0;
            Vector2 targetPosition = targetHitbox.TopLeft();
            Vector2 targetSize = targetHitbox.Size();
            for (int i = 0; i < boundaryPoints.Length - 1; i++)
            {
                if (Collision.CheckAABBvLineCollision(targetPosition, targetSize, boundaryPoints[i], boundaryPoints[i + 1], WallThickness, ref collisionPoint))
                    return true;
            }
            return false;
        }

        private void CastBoundaryLight()
        {
            if (boundaryPoints == null)
                return;

            DelegateMethods.v3_1 = Color.OrangeRed.ToVector3() * 2f;
            for (int i = 0; i < boundaryPoints.Length - 1; i++)
            {
                Utils.PlotTileLine(boundaryPoints[i], boundaryPoints[i + 1], 16f, DelegateMethods.CastLight);
            }
        }

        // ─────────────────────────────────────────────────────────────────────────────


        // Original 4-piece shader data
        public static ArmorShaderData data;
        float modifiedTime;

        public override bool PreDraw(ref Color lightColor)
        {
            // ── WIP: Rounded wall draw (ai[0] == 4) ──────────────────────────────────
            if ((int)Projectile.ai[0] == RoundedControllerStyle)
            {
                if (arenaSize == Vector2.Zero)
                    return false;

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

                if (RoundedFirewallEffect == null)
                {
                    RoundedFirewallEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/MarilithRoundedFirewall", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
                }

                RoundedFirewallEffect.Parameters["uTime"].SetValue(Main.GlobalTimeWrappedHourly);
                RoundedFirewallEffect.Parameters["uArenaSize"].SetValue(arenaSize);
                RoundedFirewallEffect.Parameters["uCornerRadius"].SetValue(CornerRadius);
                RoundedFirewallEffect.Parameters["uWallThickness"].SetValue(WallThickness);
                RoundedFirewallEffect.Parameters["uVisualPadding"].SetValue(VisualPadding);
                RoundedFirewallEffect.Parameters["uProgress"].SetValue(progress / 100f);
                RoundedFirewallEffect.Parameters["uCloudProgress"].SetValue(cloudProgress / 300f);
                RoundedFirewallEffect.CurrentTechnique.Passes[0].Apply();

                Rectangle destination = new Rectangle(
                    (int)(Projectile.position.X - Main.screenPosition.X),
                    (int)(Projectile.position.Y - Main.screenPosition.Y),
                    Projectile.width,
                    Projectile.height);
                Main.spriteBatch.Draw(tsorcRevamp.NoiseTurbulent, destination, tsorcRevamp.NoiseTurbulent.Bounds, Color.White);

                UsefulFunctions.RestartSpritebatch(ref Main.spriteBatch);
                return false;
            }
            // ─────────────────────────────────────────────────────────────────────────

            // Original 4-piece draw using FireWallShader.
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            // Apply the shader, caching it as well.
            if (data == null)
            {
                data = new ArmorShaderData(new Ref<Effect>(ModContent.Request<Effect>("tsorcRevamp/Effects/FireWallShader", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value), "FireWallShaderPass");
            }

            // Pass relevant data to the shader via these parameters.
            data.UseSaturation(Projectile.ai[0]);
            data.UseSecondaryColor(progress, cloudProgress, modifiedTime);
            if (Projectile.ai[0] == 2)
            {
                modifiedTime += 1 - (cloudProgress / 300f);
            }

            data.Apply(null);

            SpriteEffects spriteEffects = SpriteEffects.None;
            if (Projectile.spriteDirection == -1)
                spriteEffects = SpriteEffects.FlipHorizontally;

            Rectangle sourceRectangle = new Rectangle(0, 0, (int)Projectile.width, Projectile.height);
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = sourceRectangle.Size() / 2f;
            float rotation = Projectile.rotation;

            Main.EntitySpriteDraw(tsorcRevamp.NoiseTurbulent, drawPosition, sourceRectangle, Color.White, rotation, origin, Projectile.scale, spriteEffects, 0);

            rotation += MathHelper.Pi;
            if (Projectile.ai[0] == 0)
                drawPosition.X -= 140;
            if (Projectile.ai[0] == 1)
                drawPosition.X += 140;
            if (Projectile.ai[0] == 3)
                drawPosition.Y += 140;
            if (Projectile.ai[0] == 2)
            {
                rotation -= MathHelper.Pi;
                drawPosition.Y -= 140;
                spriteEffects = SpriteEffects.FlipVertically;
            }

            Main.EntitySpriteDraw(tsorcRevamp.NoiseTurbulent, drawPosition, sourceRectangle, Color.White, rotation, origin, Projectile.scale, spriteEffects, 0);

            UsefulFunctions.RestartSpritebatch(ref Main.spriteBatch);

            return false;
        }
    }
}
