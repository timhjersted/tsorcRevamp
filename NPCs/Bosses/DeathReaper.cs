using System.IO;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Projectiles.Enemy.Death;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Bosses
{
    /// <summary>
    /// Damageable Reaper summon used by Death's second phase formation attacks.
    /// ai[0] = owner NPC index
    /// ai[1] = signed slot number; positive rotates clockwise, negative counter-clockwise
    /// ai[2] = formation center X
    /// ai[3] = formation center Y
    /// </summary>
    class DeathReaper : ModNPC
    {
        const float FormationRadius = 300f;
        const float OrbitRadiansPerSecond = MathHelper.PiOver2;
        const float SpiralRadiusPerSecond = -300f;
        const float LaserSpeed = 20f;
        const int LaserDamage = 333;
        const int FormationTicks = 60;
        const int IdleEndTicks = 90;
        internal const int OrbitEndTicks = 330;
        const int FreezeEndTicks = 420;
        const int SpiralEndTicks = 540;
        const int LifetimeTicks = 600;
        const int ScatterModeBit = 1 << 17;

        public override string Texture => "Terraria/Images/NPC_" + NPCID.Reaper;
        // PLACEHOLDER sprite (vanilla Reaper).

        int EncodedSlot => (int)NPC.ai[1];
        int Slot => EncodedSlot & 0xFF;
        int SlotCount => System.Math.Max(1, (EncodedSlot >> 8) & 0xFF);
        int RotationDirection => (EncodedSlot & (1 << 16)) != 0 ? 1 : -1;
        bool ScatterMode => (EncodedSlot & ScatterModeBit) != 0;
        int Age => (int)NPC.localAI[0];
        Vector2 FormationCenter => new Vector2(NPC.ai[2], NPC.ai[3]);
        float BaseAngle => Slot * MathHelper.TwoPi / SlotCount;
        float intendedFacingAngle;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 3;
        }

        public override void SetDefaults()
        {
            NPC.aiStyle = -1;
            NPC.npcSlots = 0f;
            NPC.width = 32;
            NPC.height = 48;
            NPC.lifeMax = 1200;
            NPC.life = 1200;
            NPC.damage = 74;
            NPC.defense = 30;
            NPC.knockBackResist = 0f;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.behindTiles = true;
            NPC.friendly = false;
            NPC.dontTakeDamage = false;
            NPC.dontTakeDamageFromHostiles = true;
            NPC.chaseable = true;
            NPC.ShowNameOnHover = true;
        }

        public override void AI()
        {
            DeathBossBase owner = GetOwnerBoss();
            if (owner == null)
            {
                NPC.active = false;
                return;
            }

            if (Age == 0)
            {
                ConfigureOwnerStats(owner);
            }

            NPC.localAI[0]++;
            if (Age % 30 == 0)
            {
                NPC.netUpdate = true;
            }
            NPC.velocity = Vector2.Zero;

            Color lightColor = GetLightColor();
            Lighting.AddLight(NPC.Center, lightColor.ToVector3() * 1.4f);

            if (ScatterMode)
            {
                TickScatterReaper();
                ApplyConfiguredFacing(owner);
                return;
            }

            if (Age < FormationTicks)
            {
                TickFormation();
            }
            else if (Age < IdleEndTicks)
            {
                SetPose(BaseAngle, FormationRadius);
                intendedFacingAngle = BaseAngle;
                NPC.alpha = 0;
            }
            else if (Age < OrbitEndTicks)
            {
                TickOrbit();
            }
            else if (Age < FreezeEndTicks)
            {
                SetPose(BaseAngle, FormationRadius);
                intendedFacingAngle = BaseAngle;
                if ((Age - OrbitEndTicks) % GetLaserInterval() == 0)
                {
                    FireConfiguredLasers(BaseAngle);
                }
            }
            else if (Age < SpiralEndTicks)
            {
                TickSpiral();
            }
            else if (Age < LifetimeTicks)
            {
                TickFadeOutSpiral();
            }
            else
            {
                NPC.active = false;
            }

            ApplyConfiguredFacing(owner);
        }

        void TickScatterReaper()
        {
            DeathBossBase owner = GetOwnerBoss();
            if (owner == null)
            {
                NPC.active = false;
                return;
            }

            int layer = Slot / 3;
            int layerSlot = Slot % 3;
            float radius = layer == 0 ? owner.ScatterInnerRadius : owner.ScatterOuterRadius;
            float baseAngle = layerSlot * MathHelper.TwoPi / 3f + (layer == 1 ? MathHelper.Pi / 3f : 0f);
            int direction = layer == 0 ? 1 : -1;

            if (Age < FormationTicks)
            {
                float progress = MathHelper.SmoothStep(0f, 1f, Age / (float)FormationTicks);
                Vector2 destination = owner.NPC.Center + baseAngle.ToRotationVector2() * radius;
                NPC.Center = Vector2.Lerp(FormationCenter, destination, progress);
                intendedFacingAngle = baseAngle;
                NPC.alpha = (int)MathHelper.Lerp(255f, 0f, MathHelper.Clamp(Age / 45f, 0f, 1f));
                return;
            }

            float angle = baseAngle + NPC.localAI[1];
            Vector2 position = owner.NPC.Center + angle.ToRotationVector2() * radius;
            NPC.Center = position;
            intendedFacingAngle = angle;
            NPC.alpha = 0;

            if (Age < SpiralEndTicks)
            {
                int fireStart = layer == 0 ? owner.ScatterInnerStartTicks : owner.ScatterOuterStartTicks;
                int fireInterval = Math.Max(1, owner.ScatterFireIntervalTicks);
                if (Age >= fireStart && (Age - fireStart) % fireInterval == 0)
                {
                    FireScatterSickles(angle, owner);
                    int wave = (Age - fireStart) / fireInterval;
                    NPC.localAI[2] = Math.Max(1, owner.ScatterTurnTicks);
                    NPC.localAI[3] = MathHelper.ToRadians(RollTurnDegrees(owner.ScatterRotationSeed, layer, wave, owner));
                }
            }

            if (Age < owner.ScatterFadeStartTicks && NPC.localAI[2] > 0f)
            {
                float turnPerTick = NPC.localAI[3] / Math.Max(1, owner.ScatterTurnTicks);
                NPC.localAI[1] += direction * turnPerTick;
                NPC.localAI[2]--;
            }

            if (Age >= owner.ScatterFadeStartTicks)
            {
                if (Age >= owner.ScatterDurationTicks)
                {
                    NPC.active = false;
                    return;
                }

                float fade = (Age - owner.ScatterFadeStartTicks) / (float)(owner.ScatterDurationTicks - owner.ScatterFadeStartTicks);
                NPC.alpha = (int)MathHelper.Lerp(0f, 255f, fade);
            }
        }

        static float RollTurnDegrees(int seed, int layer, int wave, DeathBossBase owner)
        {
            uint hash = (uint)seed;
            hash ^= (uint)(layer + 1) * 0x9E3779B9u;
            hash ^= (uint)(wave + 1) * 0x85EBCA6Bu;
            hash ^= hash >> 16;
            hash *= 0x7FEB352Du;
            hash ^= hash >> 15;
            hash *= 0x846CA68Bu;
            hash ^= hash >> 16;

            float t = (hash & 0xFFFF) / 65535f;
            return MathHelper.Lerp(owner.ScatterTurnMinDegrees, owner.ScatterTurnMaxDegrees, t);
        }

        void FireScatterSickles(float angle, DeathBossBase owner)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            FireSickleFan(angle, owner.ScatterSickleCount, owner.ScatterSickleSpreadDegrees, owner.ScatterSickleSpeed, owner);
            if (owner.ScatterExtraSickleCount > 0)
            {
                FireSickleFan(angle, owner.ScatterExtraSickleCount, owner.ScatterExtraSickleSpreadDegrees, owner.ScatterExtraSickleSpeed, owner);
            }
        }

        void FireSickleFan(float angle, int count, float spreadDegrees, float speed, DeathBossBase owner)
        {
            count = Math.Max(1, count);
            float spread = MathHelper.ToRadians(spreadDegrees);
            float startAngle = angle - (count - 1) * spread * 0.5f;

            for (int i = 0; i < count; i++)
            {
                Vector2 direction = (startAngle + spread * i).ToRotationVector2();
                Projectile.NewProjectile(
                    NPC.GetSource_FromAI(),
                    NPC.Center,
                    direction * speed,
                    ModContent.ProjectileType<DeathSickleProjectile>(),
                    owner.ReaperSickleDamage,
                    0f,
                    Main.myPlayer,
                    direction.X,
                    direction.Y,
                    speed);
            }
        }

        void TickFormation()
        {
            float progress = MathHelper.SmoothStep(0f, 1f, Age / (float)FormationTicks);
            Vector2 start = FormationCenter;
            Vector2 destination = FormationCenter + BaseAngle.ToRotationVector2() * FormationRadius;

            NPC.Center = Vector2.Lerp(start, destination, progress);
            intendedFacingAngle = BaseAngle;
            NPC.alpha = (int)MathHelper.Lerp(255f, 0f, MathHelper.Clamp(Age / 45f, 0f, 1f));
        }

        void TickOrbit()
        {
            int orbitAge = Age - IdleEndTicks;
            float angle = BaseAngle + RotationDirection * orbitAge / 60f * OrbitRadiansPerSecond;
            SetPose(angle, FormationRadius);
            intendedFacingAngle = angle;

            int burstInterval = GetBurstInterval();
            int groupAge = orbitAge % GetOrbitVolleyInterval();
            if (groupAge == 0)
            {
                FireConfiguredLasers(angle, allowMultiShot: false);
                NPC.localAI[1] = Math.Max(0, GetBurstShotCount() - 1);
                NPC.localAI[2] = burstInterval;
            }
            else
            {
                if (NPC.localAI[2] > 0f)
                {
                    NPC.localAI[2]--;
                }

                if (NPC.localAI[2] <= 0f && NPC.localAI[1] > 0f)
                {
                    FireConfiguredLasers(angle, allowMultiShot: false);
                    NPC.localAI[1]--;
                    NPC.localAI[2] = burstInterval;
                }
            }
        }

        void TickSpiral()
        {
            int spiralAge = Age - FreezeEndTicks;
            float angle = BaseAngle + RotationDirection * spiralAge / 60f * OrbitRadiansPerSecond;
            float radius = FormationRadius + spiralAge / 60f * SpiralRadiusPerSecond;
            SetSignedPose(angle, radius);
            intendedFacingAngle = angle;

            if (spiralAge % GetLaserInterval() == 0)
            {
                FireConfiguredLasers(angle);
            }
        }

        void TickFadeOutSpiral()
        {
            int spiralAge = Age - FreezeEndTicks;
            float angle = BaseAngle + RotationDirection * spiralAge / 60f * OrbitRadiansPerSecond;
            float radius = FormationRadius + spiralAge / 60f * SpiralRadiusPerSecond;
            SetSignedPose(angle, radius);
            intendedFacingAngle = angle;

            float fade = (Age - SpiralEndTicks) / (float)(LifetimeTicks - SpiralEndTicks);
            NPC.alpha = (int)MathHelper.Lerp(0f, 255f, fade);
        }

        void SetPose(float angle, float radius)
        {
            Vector2 radial = angle.ToRotationVector2();
            NPC.Center = FormationCenter + radial * radius;
        }

        void SetSignedPose(float angle, float radius)
        {
            Vector2 radial = angle.ToRotationVector2();
            NPC.Center = FormationCenter + radial * radius;
        }

        void FireConfiguredLasers(float angle, bool allowMultiShot = true)
        {
            DeathBossBase owner = GetOwnerBoss();
            int count = allowMultiShot ? owner?.ReaperLaserCount ?? 1 : 1;
            if (count <= 1)
            {
                FireLaser(angle);
                return;
            }

            float spread = MathHelper.ToRadians(owner?.ReaperLaserSpreadDegrees ?? 0f);
            float startAngle = angle - (count - 1) * spread * 0.5f;
            for (int i = 0; i < count; i++)
            {
                FireLaser(startAngle + spread * i);
            }
        }

        int GetBurstInterval()
        {
            return Math.Max(1, GetOwnerBoss()?.ReaperBurstIntervalTicks ?? 5);
        }

        int GetOrbitVolleyInterval()
        {
            return Math.Max(1, GetOwnerBoss()?.ReaperOrbitVolleyIntervalTicks ?? 60);
        }

        int GetLaserInterval()
        {
            return Math.Max(1, GetOwnerBoss()?.ReaperLaserIntervalTicks ?? 8);
        }

        int GetBurstShotCount()
        {
            return Math.Max(1, GetOwnerBoss()?.ReaperBurstShotCount ?? 3);
        }

        void FireLaser(float angle)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            Vector2 direction = angle.ToRotationVector2();
            DeathBossBase owner = GetOwnerBoss();
            if (owner != null && owner.ReaperStrong2UsesSickles)
            {
                float speed = owner.ReaperStrong2SickleSpeed;
                Projectile.NewProjectile(
                    NPC.GetSource_FromAI(),
                    NPC.Center,
                    direction * speed,
                    ModContent.ProjectileType<DeathSickleProjectile>(),
                    owner.ReaperSickleDamage,
                    0f,
                    Main.myPlayer,
                    direction.X,
                    direction.Y,
                    speed);
                return;
            }

            Projectile.NewProjectile(
                NPC.GetSource_FromAI(),
                NPC.Center,
                direction * LaserSpeed,
                ModContent.ProjectileType<DeathReaperLaser>(),
                LaserDamage,
                0f,
                Main.myPlayer);
        }

        DeathBossBase GetOwnerBoss()
        {
            int ownerIndex = (int)NPC.ai[0];
            // Treat an inactive owner slot as missing so reapers despawn with the boss.
            if (ownerIndex >= 0 && ownerIndex < Main.maxNPCs && Main.npc[ownerIndex].active)
            {
                return Main.npc[ownerIndex].ModNPC as DeathBossBase;
            }

            return null;
        }

        void ConfigureOwnerStats(DeathBossBase owner)
        {
            int difficultyMultiplier = Main.masterMode ? 3 : Main.expertMode ? 2 : 1;
            NPC.lifeMax = owner.ReaperLifeMax * difficultyMultiplier;
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                NPC.life = NPC.lifeMax;
            }
            NPC.defense = owner.ReaperDefenseStat;
            EnemyDamage.SetContact(NPC, owner.ReaperContactDamage);
            NPC.netUpdate = true;
        }

        void ApplyConfiguredFacing(DeathBossBase owner)
        {
            NPC.rotation = 0f;
            float facingAngle = intendedFacingAngle;
            if (owner.CurrentReaperFacingMode == DeathBossBase.ReaperFacingMode.Player && owner.NPC.HasValidTarget)
            {
                Player target = Main.player[owner.NPC.target];
                facingAngle = (target.Center - NPC.Center).ToRotation();
            }

            int direction = facingAngle.ToRotationVector2().X < 0f ? -1 : 1;
            NPC.direction = direction;
            NPC.spriteDirection = direction;
        }

        Color GetLightColor()
        {
            return GetOwnerBoss()?.ReaperLightColor ?? Color.MediumPurple;
        }

        bool ValidOwner()
        {
            int ownerIndex = (int)NPC.ai[0];
            return ownerIndex >= 0
                && ownerIndex < Main.maxNPCs
                && Main.npc[ownerIndex].active
                && Main.npc[ownerIndex].ModNPC is DeathBossBase;
        }

        public override bool CheckActive()
        {
            return false;
        }

        public override void FindFrame(int currentFrame)
        {
            NPC.frame.Y = 0;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            for (int i = 0; i < NPC.localAI.Length; i++)
            {
                writer.Write(NPC.localAI[i]);
            }
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            for (int i = 0; i < NPC.localAI.Length; i++)
            {
                NPC.localAI[i] = reader.ReadSingle();
            }
        }
    }
}
