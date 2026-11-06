using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Weapons.Melee.Halberds;

namespace tsorcRevamp.Content.Projectiles.Melee.Halberds
{
    public class DarkrootHalberdHeld : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Content/Items/Weapons/Melee/Halberds/DarkrootHalberd";
        int Step => (int)Projectile.ai[0];
        int Age => (int)Projectile.ai[1];
        int Duration => Math.Max(1, (int)Projectile.ai[2]);
        bool Owner => Projectile.owner == Main.myPlayer;
        int facing, ordinal;
        float entry = DarkrootHalberdMotion.Carry;
        bool buffered, completed, geometryReady, previousLive, playedSwing, pushing, slammed;

        // Hold-to-charge: resolved once per item use (tap = swing, still held at ChargeHoldTicks = charge).
        bool holdResolved;
        // Thrust push and dash travel stop for good once the spear connects or the body would enter an enemy.
        bool movementStopped;
        bool dashExitApplied;
        bool chargeReadyPlayed;
        // Owner-only dash state: the player is steered along this line with tile collision.
        Vector2 dashStart;
        Vector2 dashTopLeft;
        Vector2 dashDirection;
        float dashDistance;
        // The grip slide carried over from the previous step, decaying over the first 10 ticks of a new one,
        // so a step that starts at a different slide never pops the spear.
        float carryExtension;
        Vector2 hand, point, neck, edge, butt, oldPoint, oldNeck, oldEdge, oldButt;
        float worldAngle, extension;
        readonly Vector2[] trail = new Vector2[8];
        int trailCount;

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 300;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.ownerHitCheck = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.timeLeft = 120;
        }

        public override bool ShouldUpdatePosition() => false;
        float MotionTime => DarkrootHalberdMotion.Time(Age, Duration, Step);
        public override bool? CanDamage() => geometryReady && previousLive ? null : false;
        public override bool? CanHitNPC(NPC target) => target.friendly ? false : null;

        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            if (!player.active || player.dead || player.noItems || player.CCed
                || player.HeldItem.type != ModContent.ItemType<DarkrootHalberd>()
                || player.GetModPlayer<tsorcRevampPlayer>().isDodging
                || player.GetModPlayer<tsorcRevampPlayer>().isSwordflipping)
            { Projectile.Kill(); return; }

            if (Age >= Duration && Owner && !AdvanceState(player)) return;

            // Hold left: a fresh press (the swing this item use started) still held ChargeHoldTicks into its windup
            // trades the swing for the charge. Windups are 18+ base ticks, so this always lands before any cut.
            // Chained steps never convert, so holding through a tapped combo still chains.
            if (Owner && ordinal == 0 && Step <= 2 && !holdResolved)
            {
                if (!player.controlUseItem)
                {
                    holdResolved = true;
                }
                else if (MotionTime >= DarkrootHalberdMotion.ChargeHoldTicks)
                {
                    holdResolved = true;

                    // Re-aim at the cursor and re-express the current blade angle against that aim so the pose
                    // stays continuous while the charge levels the spear.
                    float currentAngle = DarkrootHalberdMotion.Angle(Step, MotionTime, entry);
                    float currentWorld = Projectile.velocity.ToRotation() + facing * currentAngle;
                    Vector2 chargeAim = (Main.MouseWorld - player.MountedCenter).SafeNormalize(Vector2.UnitX * facing);
                    int chargeFacing = 1;
                    if (chargeAim.X < 0f)
                    {
                        chargeFacing = -1;
                    }

                    float chargeEntry = MathHelper.WrapAngle(currentWorld - chargeAim.ToRotation()) * chargeFacing;
                    Projectile.velocity = chargeAim;
                    facing = chargeFacing;
                    Begin(player, DarkrootHalberdMotion.Charge, chargeEntry);
                }
            }

            if (Owner && Step == DarkrootHalberdMotion.Charge && !player.controlUseItem)
            {
                BeginDash(player);
            }
            Projectile.timeLeft = 120;
            int age = Age;
            Projectile.Center = player.MountedCenter;
            if (facing == 0) facing = Projectile.velocity.X < 0 ? -1 : 1;
            bool aimingWindup = Step <= 2 && Age < Duration * DarkrootHalberdMotion.Windup(Step) / DarkrootHalberdMotion.BaseDuration(Step) - 8;
            if (Owner && (aimingWindup || Step == DarkrootHalberdMotion.Charge))
            {
                Vector2 aim = (Main.MouseWorld - player.MountedCenter).SafeNormalize(Vector2.UnitX * facing);
                if (Vector2.DistanceSquared(Projectile.velocity, aim) > 0.0001f)
                { Projectile.velocity = aim; facing = aim.X < 0 ? -1 : 1; Projectile.netUpdate = true; }
            }
            player.ChangeDir(facing);
            player.heldProj = Projectile.whoAmI;
            player.itemTime = player.itemAnimation = 2;
            float time = MotionTime;
            float poseTime = DarkrootHalberdMotion.Time(age + 1, Duration, Step);
            float angle = DarkrootHalberdMotion.Angle(Step, poseTime, entry);
            worldAngle = Projectile.velocity.ToRotation() + facing * angle;
            float carryFade = MathHelper.SmoothStep(1f, 0f, MathHelper.Clamp(age / 10f, 0f, 1f));
            extension = DarkrootHalberdMotion.Extension(Step, poseTime) + carryExtension * carryFade;

            // The charge draws the arm back: full stretch, then three-quarter, then quarter as it fills.
            Player.CompositeArmStretchAmount stretch = Player.CompositeArmStretchAmount.Full;
            if (Step == DarkrootHalberdMotion.Charge)
            {
                float chargeFill = poseTime / DarkrootHalberdMotion.ChargeFullTicks;
                if (chargeFill >= 0.66f)
                {
                    stretch = Player.CompositeArmStretchAmount.Quarter;
                }
                else if (chargeFill >= 0.33f)
                {
                    stretch = Player.CompositeArmStretchAmount.ThreeQuarters;
                }
            }

            float armAngle = worldAngle - MathHelper.PiOver2;
            player.SetCompositeArmFront(true, stretch, armAngle);
            hand = player.GetFrontHandPosition(stretch, armAngle);
            // Both hands stay on a reachable point of the supplied wooden shaft, including its rear-facing sweep.
            player.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full,
                DarkrootHalberdMotion.RearArm(hand - player.MountedCenter, worldAngle, facing, extension, out _));

            oldPoint = point; oldNeck = neck; oldEdge = edge; oldButt = butt;
            point = hand + DarkrootHalberdMotion.Offset(DarkrootHalberdMotion.Point, worldAngle, facing, extension);
            neck = hand + DarkrootHalberdMotion.Offset(DarkrootHalberdMotion.Neck, worldAngle, facing, extension);
            edge = hand + DarkrootHalberdMotion.Offset(DarkrootHalberdMotion.Edge, worldAngle, facing, extension);
            butt = hand + DarkrootHalberdMotion.Offset(DarkrootHalberdMotion.Butt, worldAngle, facing, extension);
            bool live = DarkrootHalberdMotion.Live(Step, time);
            if (!geometryReady || Vector2.DistanceSquared(oldPoint, point) > 128 * 128)
            { oldPoint = point; oldNeck = neck; oldEdge = edge; oldButt = butt; }
            geometryReady = true;
            previousLive = live;
            if (live)
            {
                if (!playedSwing && !Main.dedServ) { SoundEngine.PlaySound(SoundID.Item1 with { Pitch = -0.3f, Volume = 0.8f }, hand); playedSwing = true; }
                for (int i = trail.Length - 1; i > 0; i--) trail[i] = trail[i - 1];
                trail[0] = point; trailCount = Math.Min(trail.Length, trailCount + 1);
            }
            else trailCount = Math.Max(0, trailCount - 1);

            float push = Step == 2 ? DarkrootHalberdMotion.PushDistance(time, poseTime) : 0;
            if (Owner && push > 0 && !movementStopped)
            {
                // Prefix/speed changes preserve the total ThrustPush impulse, and vanilla tile movement still stops
                // it. It ends for good if this tick's step would put the body inside an enemy.
                Rectangle nextBody = player.Hitbox;
                nextBody.X += (int)(facing * push);
                if (BodyWouldEnterEnemy(nextBody))
                {
                    movementStopped = true;
                    player.velocity.X = 0;
                }
                else
                {
                    Vector2 safe = Collision.TileCollision(player.position, new Vector2(facing * push, 0), player.width, player.height);
                    player.velocity.X = safe.X;
                    pushing = true;
                }
            }
            else if (Owner && pushing) { player.velocity.X = 0; pushing = false; }
            if (Step == DarkrootHalberdMotion.Spin || Step == DarkrootHalberdMotion.Charge)
                player.velocity.X = MathHelper.Clamp(player.velocity.X, -player.maxRunSpeed * 0.4f, player.maxRunSpeed * 0.4f);

            if (Step == DarkrootHalberdMotion.Charge && !chargeReadyPlayed && poseTime >= DarkrootHalberdMotion.ChargeFullTicks)
            {
                // Fully drawn: a ping and a burst of Darkroot green at the point.
                chargeReadyPlayed = true;
                if (!Main.dedServ)
                {
                    SoundEngine.PlaySound(SoundID.MaxMana with { Volume = 0.8f, Pitch = -0.2f }, point);
                    for (int i = 0; i < 12; i++)
                    {
                        Dust ready = Dust.NewDustPerfect(point, DustID.GreenTorch, Main.rand.NextVector2Circular(2.5f, 2.5f), 80, default, 1.3f);
                        ready.noGravity = true;
                    }
                }
            }

            if (Step == DarkrootHalberdMotion.Charge && !Main.dedServ && Main.rand.NextBool(3))
            {
                Vector2 gather = point + Main.rand.NextVector2CircularEdge(18f, 18f);
                Dust mote = Dust.NewDustPerfect(gather, DustID.GreenTorch, (point - gather) * 0.08f, 100, default, 0.9f);
                mote.noGravity = true;
            }

            if (Owner && Step == DarkrootHalberdMotion.Dash)
            {
                SteerDash(player, poseTime);
            }
            if (Owner && Step <= 2 && Age >= Duration - 12 && player.controlUseItem) buffered = true;

            // Spin slam lands on the first tick the point touches the floor, or when the arc completes in the air.
            if (Step == DarkrootHalberdMotion.SpinSlam && !slammed)
            {
                bool arcComplete = poseTime >= DarkrootHalberdMotion.SlamRotateTicks;
                Vector2 probeStart = point - new Vector2(1f, 5f);
                Vector2 probe = new Vector2(0f, 14f);
                Vector2 travel = Collision.TileCollision(probeStart, probe, 2, 2);
                bool touchedFloor = travel.Y != probe.Y || Collision.SolidCollision(point - Vector2.One, 2, 2);

                if (touchedFloor || arcComplete)
                {
                    Vector2 impact = point;
                    if (travel.Y != probe.Y)
                    {
                        impact = probeStart + travel;
                    }

                    LandSlam(player, impact, touchedFloor);
                }
            }

            if (Age < Duration) Projectile.ai[1]++;
        }

        bool AdvanceState(Player player)
        {
            float angle = DarkrootHalberdMotion.Angle(Step, DarkrootHalberdMotion.BaseDuration(Step), entry);
            if (Step == DarkrootHalberdMotion.SpinWindup)
            { Begin(player, DarkrootHalberdMotion.Spin, angle); return true; }
            if (Step == DarkrootHalberdMotion.Charge)
            {
                // Held to the cap: release on its own.
                BeginDash(player);
                return true;
            }
            if (Step == DarkrootHalberdMotion.Dash) { completed = true; Projectile.Kill(); return false; }
            if (Step == DarkrootHalberdMotion.Spin)
            {
                if (player.controlUseTile && DarkrootHalberd.TryPay(player, DarkrootHalberdMotion.Spin))
                    Begin(player, DarkrootHalberdMotion.Spin, MathHelper.WrapAngle(angle));
                else
                {
                    // Release: carry the rotation on into a slam. The slam is authored against a level aim, so
                    // re-express the current blade angle relative to level; the world angle stays continuous.
                    float aimRotation = Projectile.velocity.ToRotation();
                    float levelRotation = 0f;
                    if (facing < 0)
                    {
                        levelRotation = MathHelper.Pi;
                    }

                    float aimTilt = MathHelper.WrapAngle(aimRotation - levelRotation);
                    float levelEntry = MathHelper.WrapAngle(angle) + facing * aimTilt;
                    Projectile.velocity = Vector2.UnitX * facing;
                    Begin(player, DarkrootHalberdMotion.SpinSlam, levelEntry);
                }
                return true;
            }
            if (Step == DarkrootHalberdMotion.SpinSlam) { completed = true; Projectile.Kill(); return false; }
            var combo = player.GetModPlayer<DarkrootHalberdPlayer>();
            combo.Complete(Step);
            if ((buffered || player.controlUseItem) && DarkrootHalberd.TryPay(player, combo.NextStep))
            { Begin(player, combo.NextStep, angle); return true; }
            completed = true; Projectile.Kill(); return false;
        }

        void LandSlam(Player player, Vector2 impact, bool touchedFloor)
        {
            slammed = true;

            if (!Main.dedServ && touchedFloor)
            {
                // Same weight as the Gravelord Sword's slam: heavy landing thud, shake, stone chips and dirt.
                SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/HollowKnight/false_knight_land_1st_time") { Volume = 0.5f }, impact);
                UsefulFunctions.ScreenShake(impact, 4f, 12, distanceFalloff: 460f);
                for (int i = 0; i < 16; i++)
                {
                    Vector2 velocity = new Vector2(Main.rand.NextFloat(-3.6f, 3.6f), Main.rand.NextFloat(-4f, -1f));
                    Dust.NewDustPerfect(impact, DustID.Stone, velocity, Scale: Main.rand.NextFloat(0.8f, 1.25f));
                }

                for (int i = 0; i < 10; i++)
                {
                    Vector2 velocity = new Vector2(Main.rand.NextFloat(-4.4f, 4.4f), Main.rand.NextFloat(-3f, -0.6f));
                    Dust.NewDustPerfect(impact, DustID.Dirt, velocity, Scale: Main.rand.NextFloat(0.9f, 1.3f));
                }
            }

            if (Owner)
            {
                Vector2 eruption = DarkrootHalberdRoot.ChooseEruptionPoint(player, impact);
                int rootDamage = (int)(Projectile.damage * DarkrootHalberdMotion.RootDamageMultiplier);
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), eruption - new Vector2(0f, DarkrootHalberdRoot.SpawnHalfHeight), Vector2.Zero,
                    ModContent.ProjectileType<DarkrootHalberdRoot>(), rootDamage, Projectile.knockBack, Projectile.owner);
            }
        }

        /// <summary>Owner only. Locks the dash line: toward the cursor, up to DashRangeTiles.</summary>
        void BeginDash(Player player)
        {
            Vector2 toCursor = Main.MouseWorld - player.Center;
            float maxRange = DarkrootHalberdMotion.DashRangeTiles * 16f;
            dashDirection = toCursor.SafeNormalize(Vector2.UnitX * facing);
            dashDistance = Math.Min(toCursor.Length(), maxRange);
            dashStart = player.position;
            dashTopLeft = player.position;
            dashExitApplied = false;

            // A near-vertical dash keeps the current facing.
            if (Math.Abs(dashDirection.X) > 0.05f)
            {
                facing = 1;
                if (dashDirection.X < 0f)
                {
                    facing = -1;
                }
            }

            Projectile.velocity = dashDirection;
            Begin(player, DarkrootHalberdMotion.Dash, 0f);
            player.RemoveAllGrapplingHooks();
            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.6f, Pitch = 0.3f }, player.Center);
            }
        }

        /// <summary>
        /// Owner only. Moves the player along the dash line (ease-out over DashTravel base ticks) with tile
        /// collision. Stops for good when the spear connects (OnHitNPC), the next step would put the body inside an
        /// enemy, or a wall blocks most of the step; then hands the player back to normal physics with a small push.
        /// </summary>
        void SteerDash(Player player, float poseTime)
        {
            bool travelling = poseTime <= DarkrootHalberdMotion.DashTravel && !movementStopped;
            if (travelling)
            {
                player.fallStart = (int)(player.position.Y / 16f);
                float travelProgress = MathHelper.Clamp(poseTime / DarkrootHalberdMotion.DashTravel, 0f, 1f);
                float eased = 1f - (1f - travelProgress) * (1f - travelProgress);
                Vector2 desired = dashStart + dashDirection * dashDistance * eased;
                Vector2 delta = desired - dashTopLeft;

                Rectangle nextBody = new Rectangle((int)(dashTopLeft.X + delta.X), (int)(dashTopLeft.Y + delta.Y), player.width, player.height);
                if (BodyWouldEnterEnemy(nextBody))
                {
                    movementStopped = true;
                }
                else
                {
                    Vector2 moved = Collision.TileCollision(dashTopLeft, delta, player.width, player.height, true, true, (int)player.gravDir);
                    dashTopLeft += moved;
                    player.position = dashTopLeft;
                    player.velocity = moved;

                    bool mostlyBlocked = delta.Length() > 1f && moved.Length() < delta.Length() * 0.4f;
                    if (mostlyBlocked)
                    {
                        movementStopped = true;
                    }
                }

                return;
            }

            if (!dashExitApplied)
            {
                dashExitApplied = true;
                player.velocity = dashDirection * 3f;
            }
        }

        /// <summary>True if body overlaps a hostile enemy's hitbox that the player's current hitbox does not.</summary>
        bool BodyWouldEnterEnemy(Rectangle body)
        {
            Rectangle current = Main.player[Projectile.owner].Hitbox;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!UsefulFunctions.IsHostileThreat(npc))
                {
                    continue;
                }

                if (body.Intersects(npc.Hitbox) && !current.Intersects(npc.Hitbox))
                {
                    return true;
                }
            }

            return false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // The thrust and the dash end where the spear connects, so the player never runs into what they hit.
            if (Step == 2 || Step == DarkrootHalberdMotion.Dash)
            {
                movementStopped = true;
            }
        }

        void Begin(Player player, int step, float from)
        {
            carryExtension = extension - DarkrootHalberdMotion.Extension(step, 0f);
            movementStopped = false;
            chargeReadyPlayed = false;
            slammed = false;
            Projectile.ai[0] = step;
            Projectile.ai[1] = 0;
            Projectile.ai[2] = DarkrootHalberd.Duration(player, step);
            entry = from;
            ordinal++;
            buffered = playedSwing = previousLive = false;
            trailCount = 0;
            Array.Clear(Projectile.localNPCImmunity, 0, Projectile.localNPCImmunity.Length);
            Projectile.netUpdate = true;
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
            => modifiers.SourceDamage *= DarkrootHalberdMotion.DamageMultiplier(Step);

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (!geometryReady) return false;
            float collisionPoint = 0;
            for (int i = 0; i <= 8; i++)
            {
                float fraction = i / 8f;
                Vector2 a = Vector2.Lerp(oldNeck, neck, fraction);
                Vector2 b = Vector2.Lerp(oldPoint, point, fraction);
                Vector2 c = Vector2.Lerp(oldEdge, edge, fraction);
                Vector2 shaftEnd = Vector2.Lerp(oldButt, butt, fraction);

                // Head (neck to point, neck to axe edge) plus the whole shaft from the butt up to the head.
                if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), a, b, 10, ref collisionPoint)
                    || Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), a, c, 12, ref collisionPoint)
                    || Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), shaftEnd, a, 8, ref collisionPoint))
                {
                    return true;
                }
            }
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            if (!Owner) return;
            Player player = Main.player[Projectile.owner];
            if (!completed || Step >= DarkrootHalberdMotion.SpinWindup) player.GetModPlayer<DarkrootHalberdPlayer>().ResetCombo();
            player.itemTime = player.itemAnimation = 0;
        }

        public override void SendExtraAI(BinaryWriter writer)
        { writer.Write(facing); writer.Write(entry); writer.Write(ordinal); }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            facing = reader.ReadInt32(); entry = reader.ReadSingle(); int incoming = reader.ReadInt32();
            if (incoming != ordinal)
            {
                ordinal = incoming; playedSwing = previousLive = geometryReady = slammed = chargeReadyPlayed = false; trailCount = 0;
                carryExtension = extension - DarkrootHalberdMotion.Extension(Step, 0f);
                Array.Clear(Projectile.localNPCImmunity, 0, Projectile.localNPCImmunity.Length);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (!geometryReady) return false;
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            for (int i = 1; i < trailCount; i++)
                Utils.DrawLine(Main.spriteBatch, trail[i - 1], trail[i], new Color(90, 150, 105) * (0.22f * (1 - i / (float)trailCount)), new Color(90, 150, 105) * 0.05f, 3);
            Vector2 grip = DarkrootHalberdMotion.GripAt(extension);
            SpriteEffects flip = SpriteEffects.None;
            float native = DarkrootHalberdMotion.NativeAngle;
            if (facing < 0) { flip = SpriteEffects.FlipVertically; grip.Y = texture.Height - grip.Y; native = -native; }
            Main.EntitySpriteDraw(texture, hand - Main.screenPosition, null, lightColor,
                worldAngle - native, grip, DarkrootHalberdMotion.DrawScale, flip, 0);
            return false;
        }
    }
}
