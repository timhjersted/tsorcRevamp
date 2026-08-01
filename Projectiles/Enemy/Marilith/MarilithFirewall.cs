using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using tsorcRevamp.NPCs.Bosses.SuperHardMode.Fiends;

namespace tsorcRevamp.Projectiles.Enemy.Marilith
{
    public class MarilithFirewall : ModProjectile
    {
        public const int RoundedControllerStyle = 4;

        private const float WallThickness = 140f;
        private const float CornerRadius = 30f * 16f;
        private const float VisualPadding = 240f;
        private const int CornerSegments = 12;

        private int progress;
        private float cloudProgress;
        private Vector2 arenaCenter;
        private Vector2 arenaSize;
        private Vector2[] boundaryPoints;
        private Vector2 cachedBoundaryCenter;
        private Vector2 cachedBoundarySize;

        public static Effect RoundedFirewallEffect;

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

        public override void AI()
        {
            Projectile.timeLeft = 2;
            if (!NPC.AnyNPCs(ModContent.NPCType<FireFiendMarilith>()))
            {
                Projectile.Kill();
                return;
            }

            // Old saves cannot retain these projectiles, but rejecting legacy side pieces also prevents
            // a mixed rectangular/rounded wall if this code is ever reloaded while the boss is alive.
            if ((int)Projectile.ai[0] != RoundedControllerStyle)
            {
                Projectile.Kill();
                return;
            }

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
        }

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
            {
                return false;
            }

            float collisionPoint = 0;
            Vector2 targetPosition = targetHitbox.TopLeft();
            Vector2 targetSize = targetHitbox.Size();
            for (int i = 0; i < boundaryPoints.Length - 1; i++)
            {
                if (Collision.CheckAABBvLineCollision(targetPosition, targetSize, boundaryPoints[i], boundaryPoints[i + 1], WallThickness, ref collisionPoint))
                {
                    return true;
                }
            }

            return false;
        }

        private void CastBoundaryLight()
        {
            if (boundaryPoints == null)
            {
                return;
            }

            DelegateMethods.v3_1 = Color.OrangeRed.ToVector3() * 2f;
            for (int i = 0; i < boundaryPoints.Length - 1; i++)
            {
                Utils.PlotTileLine(boundaryPoints[i], boundaryPoints[i + 1], 16f, DelegateMethods.CastLight);
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return IntersectsRoundedWall(targetHitbox);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (arenaSize == Vector2.Zero)
            {
                return false;
            }

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
    }
}
