using Microsoft.Xna.Framework;
using System;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Enemies
{
    /// <summary>
    /// A naturally spawning Super Hardmode Crimson swarm. One world spawn unfolds into 25 locusts,
    /// then the colony visibly reproduces until it reaches 100. Free locusts fly as a flock; striking
    /// one scatters nearby survivors, which reform into one or (sometimes) two attack groups; a
    /// subset instead lands on a validated floor perch, idles, then wakes by proximity or timeout.
    /// Contact deals 30 damage, adds one Destined Death stack, and latches the locust to that side of
    /// the player, refreshing Acid Venom and Obstructed while it feeds. Face the latched side and
    /// swing to kill it, or dodge roll to shake it off alive.
    /// </summary>
    public class Locust : ModNPC
    {
        public override string Texture => "tsorcRevamp/NPCs/Enemies/SuperHardMode/Locust";

        const int InitialSwarmSize = 25;
        public const int PopulationCap = 100;
        const int ArrivalTicks = 30;
        const int ReproductionTellTicks = 30;
        const int FleeTicks = 90;
        const int WakeTellTicks = 12;
        const int PerchIdleTicks = 10 * 60;
        const int ContactDamage = 30;
        const float InitialSpreadRadius = 4f * 16f; //An eight-by-eight-tile circle.
        const float NeighborRadius = 176f;
        const float SeparationRadius = 30f;
        const float PerchMinDistance = 10f * 16f;
        const float PerchMaxDistance = 15f * 16f;
        const float PerchWakeDistance = 5f * 16f;

        enum Mode : byte
        {
            Arriving,
            Swarming,
            Fleeing,
            Attached,
            Reproducing,
            SeekingPerch,
            Perched,
            Awakening
        }

        Mode mode = Mode.Arriving;
        int modeTimer;
        int breedTimer;
        int groupId;
        int attachedPlayer = -1;
        int attachSide = 1;
        Vector2 perchPoint;
        int idleWalkDirection = 1;
        int flankSide;
        bool initialized;

        public bool IsAttached => mode == Mode.Attached;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 11;
        }

        public override void SetDefaults()
        {
            NPC.width = 18;
            NPC.height = 18;
            NPC.aiStyle = -1;
            NPC.damage = ContactDamage;
            NPC.defense = 0;
            NPC.lifeMax = 300;
            NPC.value = 25;
            NPC.knockBackResist = 0.9f;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.alpha = 255;
            NPC.scale = 0.6f;
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            if (!tsorcRevampWorld.SuperHardMode || !spawnInfo.Player.ZoneCrimson || spawnInfo.Water
                || CountActive() >= PopulationCap)
            {
                return 0f;
            }

            return 0.2f;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write((byte)mode);
            writer.Write((short)modeTimer);
            writer.Write(groupId);
            writer.Write((short)attachedPlayer);
            writer.Write((sbyte)attachSide);
            writer.Write(perchPoint.X);
            writer.Write(perchPoint.Y);
            writer.Write((sbyte)idleWalkDirection);
            writer.Write((sbyte)flankSide);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            mode = (Mode)reader.ReadByte();
            modeTimer = reader.ReadInt16();
            groupId = reader.ReadInt32();
            attachedPlayer = reader.ReadInt16();
            attachSide = reader.ReadSByte();
            perchPoint = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            idleWalkDirection = reader.ReadSByte();
            flankSide = reader.ReadSByte();
        }

        public override void AI()
        {
            InitializeSwarmMember();

            if (mode != Mode.Attached && Main.netMode != NetmodeID.MultiplayerClient)
            {
                int oldTarget = NPC.target;
                NPC.TargetClosest(false);
                if (NPC.target != oldTarget)
                {
                    NPC.netUpdate = true;
                }
            }

            int targetIndex = NPC.target >= 0 && NPC.target < Main.maxPlayers
                ? NPC.target
                : Player.FindClosest(NPC.position, NPC.width, NPC.height);
            Player target = Main.player[targetIndex];
            NPC.damage = mode == Mode.Swarming ? ContactDamage : 0;
            NPC.dontTakeDamage = mode == Mode.Arriving;
            NPC.noGravity = mode != Mode.Perched;
            NPC.noTileCollide = mode != Mode.Perched;

            if (!Main.dedServ)
            {
                Lighting.AddLight(NPC.Center, 0.22f, 0.015f, 0.015f);
            }

            switch (mode)
            {
                case Mode.Arriving:
                    RunArrival();
                    break;
                case Mode.Swarming:
                    RunSwarm(target);
                    break;
                case Mode.Fleeing:
                    RunFlee();
                    break;
                case Mode.Attached:
                    RunAttached();
                    break;
                case Mode.Reproducing:
                    RunReproductionTell();
                    break;
                case Mode.SeekingPerch:
                    RunSeekingPerch(target);
                    break;
                case Mode.Perched:
                    RunPerched(target);
                    break;
                case Mode.Awakening:
                    RunAwakening(target);
                    break;
            }
        }

        void InitializeSwarmMember()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            groupId = (int)NPC.ai[1];
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            if (groupId == 0)
            {
                groupId = NewGroupId();
                NPC.ai[1] = groupId;
            }

            breedTimer = Main.rand.Next(90, 151);

            //ai[0] is the seed guard. Natural spawns arrive with zero; every generated member is
            //born with one so it cannot recursively create another initial pack of 25.
            if (NPC.ai[0] == 0f)
            {
                SeedInitialSwarm();
            }

            NPC.netUpdate = true;
        }

        void SeedInitialSwarm()
        {
            NPC.ai[0] = 1f;
            Vector2 origin = FindOpenPosition(NPC.Center - new Vector2(0f, 96f), NPC.Center);
            NPC.Center = origin;
            NPC.velocity = Main.rand.NextVector2Circular(1.5f, 1.5f);

            int toSpawn = Math.Min(InitialSwarmSize - 1, PopulationCap - CountActive());
            for (int i = 0; i < toSpawn; i++)
            {
                Vector2 candidate = origin + Main.rand.NextVector2Circular(InitialSpreadRadius, InitialSpreadRadius);
                candidate = FindOpenPosition(candidate, origin);
                SpawnMember(candidate, groupId);
            }
        }

        void RunArrival()
        {
            modeTimer++;
            float progress = MathHelper.Clamp(modeTimer / (float)ArrivalTicks, 0f, 1f);
            NPC.alpha = (int)MathHelper.Lerp(255f, 0f, progress);
            NPC.scale = MathHelper.Lerp(0.6f, 1f, progress);
            NPC.velocity *= 0.94f;
            NPC.velocity.Y += MathF.Sin(((float)Main.GameUpdateCount + NPC.whoAmI * 9f) * 0.08f) * 0.015f;

            if (!Main.dedServ && modeTimer % 3 == 0)
            {
                Dust smoke = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Wraith,
                    0f, 0f, 110, Color.Black, 0.85f);
                smoke.noGravity = true;
                smoke.velocity *= 0.25f;
            }

            if (modeTimer >= ArrivalTicks && Main.netMode != NetmodeID.MultiplayerClient)
            {
                ChangeMode(Mode.Swarming);
            }
        }

        void RunSwarm(Player target)
        {
            modeTimer++;
            NPC.alpha = 0;
            NPC.scale = MathHelper.Lerp(NPC.scale, 1f, 0.2f);

            if (flankSide != 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 flankPoint = target.Center + new Vector2(flankSide * 96f, -12f);
                if (modeTimer >= 90 || Vector2.DistanceSquared(NPC.Center, flankPoint) < 28f * 28f)
                {
                    flankSide = 0;
                    NPC.netUpdate = true;
                }
            }
            UpdateSwarmVelocity(target);

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            if (target.active && !target.dead && !target.immune
                && !target.GetModPlayer<tsorcRevampPlayer>().isDodging
                && NPC.Hitbox.Intersects(target.Hitbox))
            {
                attachedPlayer = target.whoAmI;
                attachSide = NPC.Center.X >= target.Center.X ? 1 : -1;
                ChangeMode(Mode.Attached);
                return;
            }

            breedTimer--;
            if (breedTimer <= 0)
            {
                breedTimer = Main.rand.Next(90, 151);
                if (CountActive() < PopulationCap)
                {
                    ChangeMode(Mode.Reproducing);
                }
            }
        }

        void UpdateSwarmVelocity(Player target)
        {
            Vector2 center = Vector2.Zero;
            Vector2 alignment = Vector2.Zero;
            Vector2 separation = Vector2.Zero;
            int neighbors = 0;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC other = Main.npc[i];
                if (!other.active || other.whoAmI == NPC.whoAmI || other.type != Type
                    || other.ModNPC is not Locust locust || locust.groupId != groupId || locust.IsAttached)
                {
                    continue;
                }

                Vector2 offset = NPC.Center - other.Center;
                float distance = offset.Length();
                if (distance > NeighborRadius)
                {
                    continue;
                }

                center += other.Center;
                alignment += other.velocity;
                neighbors++;

                if (distance > 0.01f && distance < SeparationRadius)
                {
                    separation += offset / distance * ((SeparationRadius - distance) / SeparationRadius);
                }
            }

            float groupAngle = groupId * 0.000137f
                + (float)Main.GameUpdateCount * (0.0045f + (groupId & 1) * 0.0015f);
            Vector2 groupTarget = flankSide != 0
                ? target.Center + new Vector2(flankSide * 96f, -12f)
                : modeTimer < 120
                    ? target.Center
                    : target.Center + groupAngle.ToRotationVector2() * 48f;
            Vector2 desired = (groupTarget - NPC.Center).SafeNormalize(Vector2.UnitX) * 4.6f;
            Vector2 steering = (desired - NPC.velocity) * 0.055f + separation * 0.42f;

            if (neighbors > 0)
            {
                center /= neighbors;
                alignment /= neighbors;
                steering += (center - NPC.Center) * 0.0018f;
                steering += (alignment - NPC.velocity) * 0.025f;
            }

            float flutterPhase = ((float)Main.GameUpdateCount + NPC.whoAmI * 17f) * 0.07f;
            steering += new Vector2(MathF.Sin(flutterPhase), MathF.Cos(flutterPhase * 1.31f)) * 0.075f;
            NPC.velocity += steering;
            if (NPC.velocity.Length() > 5.4f)
            {
                NPC.velocity = NPC.velocity.SafeNormalize(Vector2.UnitX) * 5.4f;
            }
        }

        void RunFlee()
        {
            modeTimer++;
            NPC.alpha = 0;
            NPC.scale = MathHelper.Lerp(NPC.scale, 1f, 0.2f);
            NPC.velocity *= 0.985f;

            if (modeTimer >= FleeTicks && Main.netMode != NetmodeID.MultiplayerClient)
            {
                ChangeMode(Mode.Swarming);
            }
        }

        void RunSeekingPerch(Player target)
        {
            modeTimer++;
            NPC.alpha = 0;
            NPC.scale = MathHelper.Lerp(NPC.scale, 1f, 0.2f);

            Vector2 toPerch = perchPoint - NPC.Center;
            Vector2 desired = toPerch.SafeNormalize(Vector2.UnitY) * MathHelper.Min(6.2f, toPerch.Length() * 0.12f + 1f);
            NPC.velocity = Vector2.Lerp(NPC.velocity, desired, 0.14f);

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            if (toPerch.LengthSquared() <= 8f * 8f)
            {
                NPC.Center = perchPoint;
                NPC.velocity = Vector2.Zero;
                idleWalkDirection = Main.rand.NextBool() ? 1 : -1;
                flankSide = 0;
                ChangeMode(Mode.Perched);
            }
            else if (modeTimer >= 180)
            {
                BeginReengagement(target);
            }
        }

        void RunPerched(Player target)
        {
            modeTimer++;
            NPC.alpha = 0;
            NPC.scale = MathHelper.Lerp(NPC.scale, 1f, 0.2f);
            NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, idleWalkDirection * 0.45f, 0.18f);
            NPC.spriteDirection = idleWalkDirection < 0 ? -1 : 1;

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            bool reachedPatrolEdge = Math.Abs(NPC.Center.X - perchPoint.X) >= 32f
                && Math.Sign(NPC.Center.X - perchPoint.X) == idleWalkDirection;
            if (reachedPatrolEdge || !HasGroundAhead(idleWalkDirection) || HasWallAhead(idleWalkDirection))
            {
                idleWalkDirection *= -1;
                NPC.netUpdate = true;
            }

            bool playerNearby = target.active && !target.dead
                && Vector2.DistanceSquared(NPC.Center, target.Center) <= PerchWakeDistance * PerchWakeDistance;
            bool lostPerch = NPC.Center.Y > perchPoint.Y + 48f;
            if (playerNearby || modeTimer >= PerchIdleTicks || lostPerch)
            {
                BeginReengagement(target);
            }
        }

        void RunAwakening(Player target)
        {
            modeTimer++;
            NPC.alpha = 0;
            NPC.scale = 1f;
            NPC.velocity *= 0.88f;
            NPC.velocity.Y -= 0.08f;

            if (!Main.dedServ && (modeTimer == 1 || modeTimer % 3 == 0))
            {
                int dustCount = modeTimer == 1 ? 5 : 1;
                for (int i = 0; i < dustCount; i++)
                {
                    Dust smoke = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Wraith,
                        Main.rand.NextFloat(-1.2f, 1.2f), Main.rand.NextFloat(-1.8f, -0.2f),
                        100, Color.Black, 0.8f);
                    smoke.noGravity = true;
                }
            }

            if (modeTimer >= WakeTellTicks && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 destination = flankSide != 0
                    ? target.Center + new Vector2(flankSide * 96f, -12f)
                    : target.Center;
                NPC.velocity = (destination - NPC.Center).SafeNormalize(Vector2.UnitX) * 4.6f;
                ChangeMode(Mode.Swarming);
            }
        }

        void BeginReengagement(Player target)
        {
            flankSide = target.active && !target.dead && Main.rand.NextBool(2)
                ? -target.direction
                : 0;
            NPC.velocity = new Vector2(0f, -2f);
            ChangeMode(Mode.Awakening);
        }

        void RunAttached()
        {
            modeTimer++;
            if (attachedPlayer < 0 || attachedPlayer >= Main.maxPlayers)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    DetachFromPlayer();
                }
                return;
            }

            Player player = Main.player[attachedPlayer];
            if (!player.active || player.dead)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    DetachFromPlayer();
                }
                return;
            }

            int slot = AttachedSlot();
            float horizontalOffset = 12f + (slot / 5) * 4f;
            float verticalOffset = -18f + (slot % 5) * 9f;
            NPC.Center = player.Center + new Vector2(attachSide * horizontalOffset, verticalOffset);
            NPC.velocity = Vector2.Zero;
            NPC.spriteDirection = -attachSide;
            NPC.dontTakeDamage = attachSide != player.direction;

            //Refresh while feeding; after detachment Terraria's ordinary buff countdown takes over.
            //The server owns the authoritative durations and the affected client mirrors them for
            //responsive local UI/vision without remote clients mutating another player's buffs.
            bool canRefreshPlayer = Main.netMode != NetmodeID.MultiplayerClient
                || player.whoAmI == Main.myPlayer;
            if (canRefreshPlayer && IsFirstAttachedToPlayer())
            {
                //Both authoritative server and owning client already execute this AI; quiet avoids
                //broadcasting two buff refresh packets every tick while preserving identical timers.
                player.AddBuff(BuffID.Venom, 60, true);
                player.AddBuff(BuffID.Obstructed, 120, true);
            }

            if (!Main.dedServ)
            {
                Dust blood = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Blood,
                    attachSide * 0.5f, 0.4f, 60, default, 0.85f);
                blood.velocity *= 0.35f;
            }

            if (player.GetModPlayer<tsorcRevampPlayer>().isDodging
                && Main.netMode != NetmodeID.MultiplayerClient)
            {
                DetachFromPlayer();
            }
        }

        void RunReproductionTell()
        {
            modeTimer++;
            NPC.alpha = 0;
            NPC.velocity *= 0.9f;
            NPC.scale = 1f + MathF.Sin(modeTimer / (float)ReproductionTellTicks * MathHelper.Pi) * 0.28f;

            if (!Main.dedServ && modeTimer % 2 == 0)
            {
                float angle = ((float)Main.GameUpdateCount * 0.22f + NPC.whoAmI) % MathHelper.TwoPi;
                Vector2 position = NPC.Center + angle.ToRotationVector2() * 18f;
                Dust smoke = Dust.NewDustPerfect(position, DustID.Wraith,
                    (NPC.Center - position) * 0.12f, 100, Color.Black, 0.8f);
                smoke.noGravity = true;
            }

            if (modeTimer < ReproductionTellTicks || Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            NPC.scale = 1f;
            if (CountActive() < PopulationCap)
            {
                Vector2 position = FindOpenPosition(
                    NPC.Center + Main.rand.NextVector2Circular(24f, 24f), NPC.Center);
                SpawnMember(position, groupId);
            }
            ChangeMode(Mode.Swarming);
        }

        void SpawnMember(Vector2 position, int memberGroup)
        {
            int spawned = NPC.NewNPC(NPC.GetSource_FromAI(), (int)position.X, (int)position.Y,
                Type, Target: NPC.target, ai0: 1f, ai1: memberGroup);
            if (spawned >= Main.maxNPCs)
            {
                return;
            }

            NPC member = Main.npc[spawned];
            member.Center = position;
            member.velocity = Main.rand.NextVector2Circular(2f, 2f);
            member.netUpdate = true;
            if (Main.netMode == NetmodeID.Server)
            {
                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, spawned);
            }
        }

        void DetachFromPlayer()
        {
            Player player = attachedPlayer >= 0 && attachedPlayer < Main.maxPlayers
                ? Main.player[attachedPlayer]
                : null;
            Vector2 inheritedVelocity = player?.velocity ?? Vector2.Zero;
            NPC.dontTakeDamage = false;
            NPC.velocity = inheritedVelocity + new Vector2(attachSide * 5f, -4f)
                + Main.rand.NextVector2Circular(1.5f, 1.5f);
            attachedPlayer = -1;
            ChangeMode(Mode.Fleeing);
        }

        void TriggerFlee(Vector2 threat, bool allowPerching)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            bool split = Main.rand.NextBool(3);
            int groupA = split ? NewGroupId() : groupId;
            int groupB = split ? NewGroupId() : groupId;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC other = Main.npc[i];
                if (!other.active || other.type != Type || Vector2.Distance(other.Center, NPC.Center) > 320f
                    || other.whoAmI == NPC.whoAmI || other.ModNPC is not Locust locust
                    || locust.mode == Mode.Attached)
                {
                    continue;
                }

                if (split)
                {
                    locust.groupId = (other.whoAmI & 1) == 0 ? groupA : groupB;
                    other.ai[1] = locust.groupId;
                }

                Vector2 foundPerch = Vector2.Zero;
                bool seekPerch = allowPerching
                    && ((other.whoAmI + NPC.whoAmI) % 3 == 0)
                    && locust.TryFindPerch(other.Center, out foundPerch);

                locust.mode = seekPerch ? Mode.SeekingPerch : Mode.Fleeing;
                locust.modeTimer = 0;
                locust.perchPoint = seekPerch ? foundPerch : Vector2.Zero;
                locust.idleWalkDirection = Main.rand.NextBool() ? 1 : -1;
                locust.flankSide = 0;
                Vector2 away = (other.Center - threat).SafeNormalize(Vector2.UnitY);
                other.velocity = away * Main.rand.NextFloat(6f, 8f)
                    + Main.rand.NextVector2Circular(1.5f, 1.5f);
                other.dontTakeDamage = false;
                other.netUpdate = true;
            }
        }

        void ChangeMode(Mode nextMode)
        {
            mode = nextMode;
            modeTimer = 0;
            NPC.netUpdate = true;
        }

        int AttachedSlot()
        {
            int slot = 0;
            for (int i = 0; i < NPC.whoAmI; i++)
            {
                NPC other = Main.npc[i];
                if (other.active && other.type == Type && other.ModNPC is Locust locust
                    && locust.mode == Mode.Attached && locust.attachedPlayer == attachedPlayer
                    && locust.attachSide == attachSide)
                {
                    slot++;
                }
            }
            return slot;
        }

        bool IsFirstAttachedToPlayer()
        {
            for (int i = 0; i < NPC.whoAmI; i++)
            {
                NPC other = Main.npc[i];
                if (other.active && other.type == Type && other.ModNPC is Locust locust
                    && locust.mode == Mode.Attached && locust.attachedPlayer == attachedPlayer)
                {
                    return false;
                }
            }
            return true;
        }

        public static int CountActive()
        {
            int count = 0;
            int type = ModContent.NPCType<Locust>();
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (Main.npc[i].active && Main.npc[i].type == type)
                {
                    count++;
                }
            }
            return count;
        }

        static int NewGroupId()
        {
            //ai[] values are floats; integers through 2^24 remain exact across synchronization.
            return Main.rand.Next(1, 16_777_216);
        }

        Vector2 FindOpenPosition(Vector2 preferred, Vector2 fallback)
        {
            for (int attempt = 0; attempt < 16; attempt++)
            {
                Vector2 candidate = attempt == 0
                    ? preferred
                    : preferred + Main.rand.NextVector2Circular(InitialSpreadRadius, InitialSpreadRadius);
                Vector2 topLeft = candidate - new Vector2(NPC.width * 0.5f, NPC.height * 0.5f);
                if (!Collision.SolidCollision(topLeft, NPC.width, NPC.height))
                {
                    return candidate;
                }
            }
            return fallback;
        }

        bool TryFindPerch(Vector2 origin, out Vector2 result)
        {
            for (int attempt = 0; attempt < 30; attempt++)
            {
                float radius = Main.rand.NextFloat(PerchMinDistance, PerchMaxDistance);
                Vector2 ringPoint = origin + Main.rand.NextFloat(MathHelper.TwoPi).ToRotationVector2() * radius;
                int tileX = (int)(ringPoint.X / 16f);
                int startTileY = (int)(ringPoint.Y / 16f);

                for (int scan = 0; scan <= 12; scan++)
                {
                    int offset = scan == 0 ? 0 : (scan + 1) / 2 * (scan % 2 == 1 ? 1 : -1);
                    int tileY = startTileY + offset;
                    if (!IsStandableSurface(tileX, tileY)
                        || (!IsStandableSurface(tileX - 1, tileY) && !IsStandableSurface(tileX + 1, tileY)))
                    {
                        continue;
                    }

                    Vector2 candidate = new(tileX * 16f + 8f, tileY * 16f - NPC.height * 0.5f - 1f);
                    float distance = Vector2.Distance(origin, candidate);
                    Vector2 topLeft = candidate - new Vector2(NPC.width * 0.5f, NPC.height * 0.5f);
                    if (distance >= PerchMinDistance && distance <= PerchMaxDistance
                        && !Collision.SolidCollision(topLeft, NPC.width, NPC.height))
                    {
                        result = candidate;
                        return true;
                    }
                }
            }

            result = Vector2.Zero;
            return false;
        }

        bool HasGroundAhead(int direction)
        {
            int tileX = (int)((NPC.Center.X + direction * 14f) / 16f);
            int feetTileY = (int)((NPC.Bottom.Y + 4f) / 16f);
            return IsStandableSurface(tileX, feetTileY - 1)
                || IsStandableSurface(tileX, feetTileY)
                || IsStandableSurface(tileX, feetTileY + 1);
        }

        bool HasWallAhead(int direction)
        {
            Vector2 probe = new(
                direction > 0 ? NPC.Right.X : NPC.Left.X - 4f,
                NPC.position.Y + 2f);
            return Collision.SolidCollision(probe, 4, Math.Max(2, NPC.height - 6));
        }

        static bool IsStandableSurface(int tileX, int tileY)
        {
            if (tileX < 5 || tileX >= Main.maxTilesX - 5 || tileY < 5 || tileY >= Main.maxTilesY - 5)
            {
                return false;
            }

            Tile tile = Framing.GetTileSafely(tileX, tileY);
            return WorldGen.SolidOrSlopedTile(tile)
                || (tile.HasTile && !tile.IsActuated
                    && (TileID.Sets.Platforms[tile.TileType] || Main.tileSolidTop[tile.TileType]));
        }

        public override bool CanHitPlayer(Player target, ref int cooldownSlot)
        {
            //The latch decision happens during AI, immediately before Terraria resolves contact.
            //Keep that one transition tick hostile so the touch still deals 30 and invokes
            //OnHitPlayer; attached locusts become harmless feeders on the following tick.
            return mode == Mode.Swarming || (mode == Mode.Attached && modeTimer == 0);
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
            if (hurtInfo.Damage > 0)
            {
                target.AddBuff(ModContent.BuffType<Buffs.Debuffs.DestinedDeath>(), 720);
            }
        }

        public override void ModifyHitByItem(Player player, Item item, ref NPC.HitModifiers modifiers)
        {
            if (mode == Mode.Attached)
            {
                modifiers.FinalDamage.Flat += NPC.lifeMax * 4f;
            }
        }

        public override void ModifyHitByProjectile(Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            if (mode == Mode.Attached && projectile.DamageType == DamageClass.Melee)
            {
                modifiers.FinalDamage.Flat += NPC.lifeMax * 4f;
            }
        }

        public override void OnHitByItem(Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            TriggerFlee(player.Center, false);
        }

        public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            Vector2 threat = projectile.owner >= 0 && projectile.owner < Main.maxPlayers
                ? Main.player[projectile.owner].Center
                : projectile.Center;
            TriggerFlee(threat, false);
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            if (NPC.life > 0)
            {
                return;
            }

            TriggerFlee(NPC.Center, true);
            if (Main.dedServ)
            {
                return;
            }

            for (int i = 0; i < 10; i++)
            {
                Dust blood = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Blood,
                    hit.HitDirection * Main.rand.NextFloat(0.5f, 2.5f), Main.rand.NextFloat(-2.5f, 1f),
                    40, default, Main.rand.NextFloat(0.8f, 1.2f));
                blood.noGravity = Main.rand.NextBool(2);
            }
        }

        public override void FindFrame(int frameHeight)
        {
            int firstFrame = mode == Mode.Perched ? 0 : mode == Mode.Attached ? 4 : 8;
            int frameCount = mode == Mode.Perched ? 4 : mode == Mode.Attached ? 4 : 3;
            int frameDuration = mode == Mode.Perched ? 8 : mode == Mode.Attached ? 6 : 5;

            NPC.frameCounter++;
            int animationFrame = (int)(NPC.frameCounter / frameDuration) % frameCount;
            NPC.frame.Y = (firstFrame + animationFrame) * frameHeight;

            if (mode != Mode.Attached && mode != Mode.Perched)
            {
                if (NPC.velocity.X < -0.05f)
                {
                    NPC.spriteDirection = -1; //The sheet is authored facing left.
                }
                else if (NPC.velocity.X > 0.05f)
                {
                    NPC.spriteDirection = 1;
                }
            }
        }
    }
}
