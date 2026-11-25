using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Projectiles.Melee.Spears;

namespace tsorcRevamp.Content.Items.Weapons.Melee.Spears.OrnsteinSpear
{
    public class OrnsteinSpearLunge : ModProjectile
    {
        private enum Phase
        {
            Charge,
            Lunge,
            Recovery
        }

        private const int FullChargeTicks = 36;
        private const int LungeTicks = 10;
        private const int RecoveryTicks = 16;
        private const float TapLungeDistance = 64f;
        private const float ChargedLungeDistance = 144f;
        private const float BoltRange = 224f;
        private const int BoltDelay = 2;
        private const int BoltTravelTicks = 10;
        private const float BoltWidth = 12f;
        private static readonly Vector2 Grip = new Vector2(24f, 80f);
        private static readonly float GripToTip = Vector2.Distance(Grip, OrnsteinSpearProj.TipOrigin) * OrnsteinSpearProj.SpriteScale;

        private Phase CurrentPhase => (Phase)(int)Projectile.ai[0];
        private int Age => (int)Projectile.ai[1];
        private bool FullyCharged => Projectile.ai[2] > 0f;
        private bool IsOwner => Projectile.owner == Main.myPlayer;
        private bool BoltActive => CurrentPhase == Phase.Recovery && boltLength > 0f
            && Age >= BoltDelay && Age < BoltDelay + BoltTravelTicks;

        private float lungeDistance;
        private bool travelStopped;
        private Vector2 boltStart;
        private float boltLength;
        private Vector2 hand;
        private Vector2 tip;
        private Vector2 previousTip;
        private Vector2 drawGrip;
        private bool poseReady;
        private bool playedFullCharge;
        private bool playedRelease;
        private bool playedDischarge;

        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(OrnsteinSpearProj));

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
        public override bool? CanDamage() => poseReady && (CurrentPhase == Phase.Lunge || BoltActive);

        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            if (!player.active || player.dead)
            {
                Projectile.Kill();
                return;
            }

            // Cursor, release and movement belong to the player who owns the item use. Other peers follow the projectile's synced phase and aim.
            if (IsOwner && (player.HeldItem.ModItem is not OrnsteinSpearItem || player.noItems || player.CCed
                || player.mount.Active || player.GetModPlayer<tsorcRevampPlayer>().isDodging
                || player.GetModPlayer<tsorcRevampPlayer>().isSwordflipping))
            {
                Projectile.Kill();
                return;
            }

            Projectile.ai[1]++;
            Vector2 fallback = Vector2.UnitX * player.direction;
            if (CurrentPhase == Phase.Charge && IsOwner)
            {
                Vector2 towardCursor = Main.MouseWorld - player.MountedCenter;
                Vector2 newAim = towardCursor.SafeNormalize(fallback);
                if (Vector2.DistanceSquared(newAim, Projectile.velocity) > 0.0001f)
                {
                    Projectile.velocity = newAim;
                    Projectile.netUpdate = true;
                }

                if (!player.controlUseTile)
                {
                    float charge = MathHelper.Clamp(Age / (float)FullChargeTicks, 0f, 1f);
                    float maxDistance = MathHelper.Lerp(TapLungeDistance, ChargedLungeDistance, charge);
                    lungeDistance = Math.Min(towardCursor.Length(), maxDistance);
                    Projectile.ai[2] = charge >= 1f ? 1f : 0f;
                    player.RemoveAllGrapplingHooks();
                    ChangePhase(Phase.Lunge);
                }
            }

            Vector2 aim = Projectile.velocity.SafeNormalize(fallback);
            if (IsOwner && CurrentPhase == Phase.Lunge)
            {
                if (Age >= LungeTicks)
                {
                    ChangePhase(Phase.Recovery);
                }
                else if (!travelStopped)
                {
                    // Ease out over ten ticks. TileCollision only checks tiles near the starting position, so check the body in 8px steps.
                    float remaining = 1f - Age / (float)LungeTicks;
                    float nextRemaining = 1f - (Age + 1) / (float)LungeTicks;
                    Vector2 wanted = aim * lungeDistance * (remaining * remaining - nextRemaining * nextRemaining);
                    int steps = Math.Max(1, (int)MathF.Ceiling(wanted.Length() / 8f));
                    Vector2 step = wanted / steps;
                    for (int i = 0; i < steps && !travelStopped; i++)
                    {
                        Vector2 movement = Collision.TileCollision(player.position, step, player.width, player.height, false, false, (int)player.gravDir);
                        Rectangle currentBody = player.Hitbox;
                        Rectangle nextBody = currentBody;
                        nextBody.Offset((int)MathF.Round(movement.X), (int)MathF.Round(movement.Y));
                        foreach (NPC npc in Main.ActiveNPCs)
                        {
                            if (UsefulFunctions.IsHostileThreat(npc) && nextBody.Intersects(npc.Hitbox) && !currentBody.Intersects(npc.Hitbox))
                            {
                                movement = Vector2.Zero;
                                break;
                            }
                        }

                        player.position += movement;
                        travelStopped = Vector2.DistanceSquared(movement, step) > 0.01f;
                    }

                    player.velocity = Vector2.Zero;
                }
            }

            if (CurrentPhase == Phase.Recovery && Age >= RecoveryTicks)
            {
                Projectile.Kill();
                return;
            }

            Projectile.timeLeft = 120;
            Projectile.Center = player.MountedCenter;
            player.heldProj = Projectile.whoAmI;
            player.itemTime = 2;
            player.itemAnimation = 2;
            if (Math.Abs(aim.X) > 0.05f)
            {
                player.ChangeDir(aim.X < 0f ? -1 : 1);
            }

            float drawBack = 0f;
            if (CurrentPhase == Phase.Charge)
            {
                drawBack = 12f * MathHelper.Clamp(Age / (float)FullChargeTicks, 0f, 1f);
            }
            else if (CurrentPhase == Phase.Recovery)
            {
                drawBack = 18f * MathHelper.Clamp(Age / (float)RecoveryTicks, 0f, 1f);
            }

            Projectile.rotation = aim.ToRotation() + MathHelper.PiOver4;
            float armRotation = aim.ToRotation() - MathHelper.PiOver2;
            Player.CompositeArmStretchAmount stretch = Player.CompositeArmStretchAmount.Full;
            if (drawBack >= 5f)
            {
                stretch = Player.CompositeArmStretchAmount.ThreeQuarters;
            }

            player.SetCompositeArmFront(true, stretch, armRotation);
            hand = player.GetFrontHandPosition(stretch, armRotation);
            drawGrip = hand - aim * drawBack;
            previousTip = tip;
            tip = drawGrip + aim * GripToTip;
            if (!poseReady || Vector2.DistanceSquared(previousTip, tip) > 128f * 128f)
            {
                previousTip = tip;
            }

            poseReady = true;
            if (!Main.dedServ)
            {
                if (CurrentPhase == Phase.Charge)
                {
                    Vector2 gather = tip + Main.rand.NextVector2CircularEdge(18f, 18f);
                    Dust mote = Dust.NewDustPerfect(gather, DustID.GoldFlame, (tip - gather) * 0.22f, 80, default, 0.85f);
                    mote.noGravity = true;
                    Lighting.AddLight(tip, 0.65f, 0.42f, 0.1f);
                    if (!playedFullCharge && Age >= FullChargeTicks)
                    {
                        playedFullCharge = true;
                        SoundEngine.PlaySound(SoundID.MaxMana with { Pitch = 0.2f }, tip);
                        for (int i = 0; i < 12; i++)
                        {
                            Dust spark = Dust.NewDustPerfect(tip, DustID.GoldFlame, Main.rand.NextVector2Circular(3f, 3f), 50);
                            spark.noGravity = true;
                        }
                    }
                }
                else if (CurrentPhase == Phase.Lunge)
                {
                    if (!playedRelease)
                    {
                        playedRelease = true;
                        SoundEngine.PlaySound(SoundID.Item74 with { Pitch = 0.2f }, hand);
                    }

                    Dust streak = Dust.NewDustPerfect(tip, DustID.GoldFlame, -aim * 3f, 70, default, 1.1f);
                    streak.noGravity = true;
                }
                else if (BoltActive)
                {
                    float progress = (Age - BoltDelay + 1) / (float)BoltTravelTicks;
                    Vector2 boltHead = boltStart + aim * (boltLength * progress);
                    if (!playedDischarge)
                    {
                        playedDischarge = true;
                        SoundEngine.PlaySound(SoundID.Item93 with { Volume = 0.65f }, boltStart);
                    }

                    for (int i = 0; i < 4; i++)
                    {
                        Dust spark = Dust.NewDustPerfect(boltHead + Main.rand.NextVector2Circular(4f, 4f),
                            DustID.GoldFlame, Main.rand.NextVector2Circular(2f, 2f), 70);
                        spark.noGravity = true;
                    }

                    Lighting.AddLight(boltHead, 0.9f, 0.58f, 0.17f);
                }
            }
        }

        private void ChangePhase(Phase phase)
        {
            Projectile.ai[0] = (float)phase;
            Projectile.ai[1] = 0f;
            Projectile.netUpdate = true;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 target = targetHitbox.Center.ToVector2();
            Vector2 aim = Projectile.rotation.ToRotationVector2().RotatedBy(-MathHelper.PiOver4);
            float collisionPoint = 0f;
            if (CurrentPhase == Phase.Lunge && poseReady)
            {
                return Collision.CanHitLine(hand, 1, 1, target, 1, 1)
                    && (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
                        tip - aim * 26f, tip, 14f, ref collisionPoint)
                    || Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
                        previousTip, tip, 14f, ref collisionPoint));
            }

            if (!BoltActive)
            {
                return false;
            }

            int boltTick = Age - BoltDelay;
            Vector2 front = boltStart + aim * (boltLength * (boltTick + 1) / BoltTravelTicks);
            Vector2 back = boltStart + aim * (boltLength * boltTick / BoltTravelTicks);
            return Collision.CanHitLine(boltStart, 1, 1, target, 1, 1)
                && Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), back, front, BoltWidth, ref collisionPoint);
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (BoltActive)
            {
                modifiers.SourceDamage *= 0.7f;
            }
            else if (FullyCharged)
            {
                modifiers.SourceDamage *= 1.35f;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (!IsOwner || CurrentPhase != Phase.Lunge)
            {
                return;
            }

            if (FullyCharged)
            {
                Vector2 aim = Projectile.velocity.SafeNormalize(Vector2.UnitX);
                float contact = Vector2.Dot(target.Center - hand, aim);
                boltStart = hand + aim * MathHelper.Clamp(contact, 0f, GripToTip);
                // Clip the discharge to the first obstruction. The same length drives the live frontier and the drawn arc.
                for (float distance = 8f; distance <= BoltRange; distance += 8f)
                {
                    Vector2 point = boltStart + aim * distance;
                    if (!Collision.CanHitLine(boltStart, 1, 1, point, 1, 1))
                    {
                        break;
                    }

                    boltLength = distance;
                }
            }

            Main.player[Projectile.owner].velocity = Vector2.Zero;
            ChangePhase(Phase.Recovery);
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.WriteVector2(boltStart);
            writer.Write(boltLength);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            boltStart = reader.ReadVector2();
            boltLength = reader.ReadSingle();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (!poseReady)
            {
                return false;
            }

            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 drawPosition = drawGrip - Main.screenPosition;
            Main.EntitySpriteDraw(texture, drawPosition, null, lightColor, Projectile.rotation,
                Grip, OrnsteinSpearProj.SpriteScale, SpriteEffects.None, 0);

            if (BoltActive)
            {
                float frontDistance = boltLength * (Age - BoltDelay + 1) / BoltTravelTicks;
                Vector2 aim = Projectile.velocity.SafeNormalize(Vector2.UnitX);
                Vector2 across = new Vector2(-aim.Y, aim.X);
                Vector2 previous = boltStart;
                for (int segment = 1; segment <= 12; segment++)
                {
                    float distance = Math.Min(segment * 20f, frontDistance);
                    float bend = segment % 2 == 0 ? -4f : 4f;
                    if (distance == frontDistance)
                    {
                        bend = 0f;
                    }

                    Vector2 next = boltStart + aim * distance + across * bend;
                    float remaining = frontDistance - distance;
                    float brightness = MathHelper.Clamp(1f - remaining / 96f, 0.2f, 1f);
                    Color glow = new Color(216, 125, 31, 0) * (0.55f * brightness);
                    Color core = new Color(255, 230, 152) * brightness;
                    Utils.DrawLine(Main.spriteBatch, previous, next, glow, glow, 6f);
                    Utils.DrawLine(Main.spriteBatch, previous, next, core, core, 2f);
                    previous = next;
                    if (distance >= frontDistance)
                    {
                        break;
                    }
                }
            }

            return false;
        }
    }
}
