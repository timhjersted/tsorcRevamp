using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Weapons.Enemy;

namespace tsorcRevamp.Content.Projectiles.Melee.Hammers
{
    public class SmoughHammerSlam : ModProjectile
    {
        private enum Phase
        {
            Setup,
            Hop,
            Fall,
            Impact
        }

        private const int HopTicks = 9;
        private const int FallTimeout = 105;
        private const int PlantTicks = 4;
        private const int WaveTicks = 22;
        private const float WaveSpeed = 8f;
        private const float ImpactOffset = 54f;
        private const float RaisedAngle = -2.3f;
        private const float StrikeAngle = 0.6f;
        private static readonly Vector2 HammerGrip = new Vector2(13f, 65f);
        private static readonly Vector2 HammerHead = new Vector2(42f, 23f);

        private Phase CurrentPhase => (Phase)(int)Projectile.ai[0];
        private int Age => (int)Projectile.ai[1];
        private bool IsOwner => Projectile.owner == Main.myPlayer;
        private int Facing => Projectile.ai[2] < 0f ? -1 : 1;
        private bool DirectHitActive => CurrentPhase == Phase.Impact && Age <= PlantTicks;
        private bool WaveActive => CurrentPhase == Phase.Impact && Age > PlantTicks;

        private Vector2 impactPoint;
        private Vector2 leftWave;
        private Vector2 rightWave;
        private int waveSteps;
        private bool leftWaveAlive;
        private bool rightWaveAlive;
        private bool playedImpact;
        private bool poseReady;
        private Vector2 drawPosition;
        private Vector2 drawOrigin;
        private float drawRotation;
        private SpriteEffects drawEffects;

        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(SmoughGreatHammer));

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.hide = true;
            Projectile.netImportant = true;
            Projectile.penetrate = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.timeLeft = 120;
        }

        public override bool ShouldUpdatePosition() => false;
        public override bool? CanCutTiles() => false;
        public override bool? CanDamage() => DirectHitActive || (WaveActive && (leftWaveAlive || rightWaveAlive));

        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            if (!player.active || player.dead)
            {
                Projectile.Kill();
                return;
            }

            if (IsOwner && (player.HeldItem.ModItem is not SmoughGreatHammer || player.noItems || player.CCed
                || player.mount.Active || player.gravDir != 1f || player.GetModPlayer<tsorcRevampPlayer>().isDodging
                || player.GetModPlayer<tsorcRevampPlayer>().isSwordflipping))
            {
                Projectile.Kill();
                return;
            }

            Projectile.ai[1]++;
            float floorDistance = 68f;
            if (CurrentPhase != Phase.Impact)
            {
                // Probe the same player footprint vanilla moves, including platforms. A ceiling hit never counts as a landing.
                floorDistance = ProbeDown(player.position, floorDistance, player.width, player.height);
            }

            if (IsOwner)
            {
                if (CurrentPhase == Phase.Setup)
                {
                    int facing = player.direction;
                    if (Math.Abs(Projectile.velocity.X) > 0.05f)
                    {
                        facing = Projectile.velocity.X < 0f ? -1 : 1;
                    }

                    Projectile.ai[2] = facing;
                    player.RemoveAllGrapplingHooks();
                    if (floorDistance <= 2f && player.velocity.Y >= 0f)
                    {
                        player.velocity = new Vector2(2.8f * facing, -6f);
                        ChangePhase(Phase.Hop);
                    }
                    else
                    {
                        ChangePhase(Phase.Fall);
                    }
                }
                else if (CurrentPhase == Phase.Hop && (Age >= HopTicks || (Age >= 2 && player.velocity.Y >= 0f)))
                {
                    ChangePhase(Phase.Fall);
                }

                if (CurrentPhase == Phase.Fall)
                {
                    if (Age > 0 && floorDistance <= 2f && player.velocity.Y >= 0f)
                    {
                        Vector2 feet = player.Bottom + new Vector2(0f, floorDistance);
                        impactPoint = feet;
                        for (float distance = ImpactOffset; distance >= 18f; distance -= 6f)
                        {
                            Vector2 ahead = feet + new Vector2(distance * Facing, 0f);
                            if (TryFloor(ahead, out Vector2 aheadFloor)
                                && Collision.CanHitLine(feet - new Vector2(0f, 12f), 1, 1, aheadFloor - new Vector2(0f, 12f), 1, 1))
                            {
                                impactPoint = aheadFloor;
                                break;
                            }
                        }

                        player.velocity = Vector2.Zero;
                        ChangePhase(Phase.Impact);
                    }
                    else if (Age >= FallTimeout)
                    {
                        Projectile.Kill();
                        return;
                    }
                    else
                    {
                        // Hand the drop to normal player physics. The floor probe decides when the weapon can land.
                        float fallSpeed = MathHelper.Lerp(4.5f, 10f, MathHelper.Clamp(Age / 8f, 0f, 1f));
                        player.velocity.Y = Math.Max(player.velocity.Y, fallSpeed);
                        player.velocity.X *= 0.91f;
                        player.fallStart = (int)(player.position.Y / 16f);
                    }
                }
                else if (CurrentPhase == Phase.Impact && Age <= PlantTicks)
                {
                    player.velocity.X = 0f;
                }
            }

            if (CurrentPhase == Phase.Impact && Age > PlantTicks + WaveTicks)
            {
                Projectile.Kill();
                return;
            }

            Projectile.timeLeft = 120;
            Projectile.Center = player.MountedCenter;
            player.heldProj = Projectile.whoAmI;
            player.itemTime = 2;
            player.itemAnimation = 2;
            if (CurrentPhase != Phase.Setup)
            {
                player.ChangeDir(Facing);
            }

            Projectile.GetGlobalProjectile<tsorcGlobalProjectile>().ProjectilePoiseMultiplier = DirectHitActive ? 2.3f : 0.65f;
            if (CurrentPhase == Phase.Impact && !playedImpact)
            {
                playedImpact = true;
                leftWave = impactPoint;
                rightWave = impactPoint;
                leftWaveAlive = true;
                rightWaveAlive = true;
                if (!Main.dedServ)
                {
                    SoundEngine.PlaySound(SoundID.DD2_MonkStaffGroundImpact with { Volume = 0.85f, Pitch = -0.35f }, impactPoint);
                    for (int i = 0; i < 20; i++)
                    {
                        Vector2 scatter = new Vector2(Main.rand.NextFloat(-5f, 5f), Main.rand.NextFloat(-5f, -1f));
                        Dust.NewDustPerfect(impactPoint - Vector2.UnitY * 3f, DustID.Stone, scatter, 40, default, 1.1f);
                    }

                    for (int i = 0; i < 18; i++)
                    {
                        Vector2 scatter = Main.rand.NextVector2Circular(6f, 3f) - Vector2.UnitY * 3f;
                        Dust spark = Dust.NewDustPerfect(impactPoint - Vector2.UnitY * 4f, DustID.GoldFlame, scatter, 50);
                        spark.noGravity = true;
                    }
                }
            }

            // Both crests share the slam's hit ledger. Advance from the synced impact on every peer, catching up if the packet arrived late.
            int desiredWaveSteps = Math.Clamp(Age - PlantTicks, 0, WaveTicks);
            while (WaveActive && waveSteps < desiredWaveSteps)
            {
                waveSteps++;
                AdvanceWave(-1, ref leftWave, ref leftWaveAlive);
                AdvanceWave(1, ref rightWave, ref rightWaveAlive);
            }

            float angle = RaisedAngle;
            if (CurrentPhase == Phase.Hop)
            {
                angle = MathHelper.SmoothStep(-0.65f, RaisedAngle, MathHelper.Clamp(Age / 6f, 0f, 1f));
            }
            else if (CurrentPhase == Phase.Fall)
            {
                // Bring the head forward as the floor approaches. High drops hold the overhead pose until the last 68px.
                float approach = MathHelper.Clamp(1f - floorDistance / 68f, 0f, 1f);
                angle = MathHelper.SmoothStep(RaisedAngle, StrikeAngle, approach);
            }
            else if (CurrentPhase == Phase.Impact)
            {
                float rise = MathHelper.Clamp((Age - PlantTicks) / 13f, 0f, 1f);
                angle = MathHelper.SmoothStep(StrikeAngle, -0.65f, rise);
            }

            Vector2 axis = new Vector2(MathF.Cos(angle) * Facing, MathF.Sin(angle));
            Vector2 plantedHead = impactPoint - Vector2.UnitY * 16f;
            if (DirectHitActive)
            {
                axis = (plantedHead - player.MountedCenter).SafeNormalize(axis);
            }

            float armRotation = axis.ToRotation() - MathHelper.PiOver2;
            player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, armRotation);
            Vector2 hand = player.GetFrontHandPosition(Player.CompositeArmStretchAmount.Full, armRotation);
            drawOrigin = HammerGrip;
            drawPosition = hand;
            if (DirectHitActive)
            {
                axis = (plantedHead - hand).SafeNormalize(axis);
                drawOrigin = HammerHead;
                drawPosition = plantedHead;
            }

            Vector2 nativeAxis = HammerHead - HammerGrip;
            drawEffects = SpriteEffects.None;
            if (Facing > 0)
            {
                // Turn the broad striking rim into the downswing. Mirror the pivot along with the source.
                nativeAxis.Y = -nativeAxis.Y;
                drawOrigin.Y = 80f - drawOrigin.Y;
                drawEffects = SpriteEffects.FlipVertically;
            }

            drawRotation = axis.ToRotation() - nativeAxis.ToRotation();
            poseReady = true;
        }

        private void ChangePhase(Phase phase)
        {
            Projectile.ai[0] = (float)phase;
            Projectile.ai[1] = 0f;
            Projectile.netUpdate = true;
        }

        private static bool TryFloor(Vector2 near, out Vector2 floor)
        {
            // A wave can step up 12px or down 14px. Start the probe in clear air to stop at vertical walls.
            Vector2 probe = near - new Vector2(1f, 14f);
            floor = near;
            if (Collision.SolidCollision(probe, 2, 2))
            {
                return false;
            }

            float down = ProbeDown(probe, 26f, 2, 2);
            if (down >= 26f)
            {
                return false;
            }

            floor.Y = probe.Y + 2f + down;
            return true;
        }

        private static float ProbeDown(Vector2 position, float distance, int width, int height)
        {
            // TileCollision only visits near the starting footprint. Advance the probe in half-tile steps until it finds support.
            float travelled = 0f;
            while (travelled < distance)
            {
                float step = Math.Min(8f, distance - travelled);
                Vector2 start = position + new Vector2(0f, travelled);
                Vector2 movement = Collision.TileCollision(start, new Vector2(0f, step), width, height);
                travelled += movement.Y;
                if (movement.Y < step)
                {
                    break;
                }
            }

            return travelled;
        }

        private void AdvanceWave(int direction, ref Vector2 edge, ref bool alive)
        {
            if (!alive)
            {
                return;
            }

            Vector2 wanted = edge + new Vector2(direction * WaveSpeed, 0f);
            alive = TryFloor(wanted, out Vector2 floor)
                && Collision.CanHitLine(edge - Vector2.UnitY * 8f, 1, 1, floor - Vector2.UnitY * 8f, 1, 1);
            if (!Main.dedServ && waveSteps >= Age - PlantTicks)
            {
                Vector2 dustBase = edge;
                if (alive)
                {
                    dustBase = floor;
                }

                float fade = 1f - waveSteps / (float)(WaveTicks + 4);
                for (int i = 0; i < 4; i++)
                {
                    Vector2 position = dustBase + new Vector2(Main.rand.NextFloat(-6f, 6f), -2f);
                    Vector2 velocity = new Vector2(direction * Main.rand.NextFloat(0.4f, 1.5f), Main.rand.NextFloat(-4.8f, -1.3f));
                    Dust.NewDustPerfect(position, DustID.Stone, velocity, 75, default, 0.65f + fade * 0.45f);
                    Dust spark = Dust.NewDustPerfect(position, DustID.GoldFlame, velocity, 75, default, 0.7f + fade * 0.35f);
                    spark.noGravity = true;
                }
            }

            if (alive)
            {
                edge = floor;
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 target = targetHitbox.Center.ToVector2();
            if (DirectHitActive)
            {
                Rectangle head = Utils.CenteredRectangle(impactPoint - new Vector2(0f, 18f), new Vector2(46f, 42f));
                return head.Intersects(targetHitbox)
                    && Collision.CanHitLine(impactPoint - new Vector2(0f, 16f), 1, 1, target, 1, 1);
            }

            if (!WaveActive)
            {
                return false;
            }

            return (leftWaveAlive && WaveHits(leftWave, targetHitbox, target))
                || (rightWaveAlive && WaveHits(rightWave, targetHitbox, target));
        }

        private static bool WaveHits(Vector2 edge, Rectangle targetHitbox, Vector2 target)
        {
            Vector2 crest = edge - new Vector2(0f, 12f);
            Rectangle wave = Utils.CenteredRectangle(crest, new Vector2(21f, 24f));
            return wave.Intersects(targetHitbox) && Collision.CanHitLine(crest, 1, 1, target, 1, 1);
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (DirectHitActive)
            {
                modifiers.SourceDamage *= 1.6f;
            }
            else
            {
                modifiers.SourceDamage *= 0.65f;
                modifiers.Knockback *= 0.6f;
            }

            if (target.Center.X != impactPoint.X)
            {
                modifiers.HitDirectionOverride = target.Center.X < impactPoint.X ? -1 : 1;
            }
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.WriteVector2(impactPoint);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            impactPoint = reader.ReadVector2();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (poseReady)
            {
                Texture2D texture = TextureAssets.Projectile[Type].Value;
                Vector2 position = drawPosition - Main.screenPosition;
                Main.EntitySpriteDraw(texture, position, null, lightColor, drawRotation, drawOrigin, 1f, drawEffects, 0);
            }

            return false;
        }
    }
}
