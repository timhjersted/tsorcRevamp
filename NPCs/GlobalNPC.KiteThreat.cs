using System;
using System.IO;
using Terraria;
using Terraria.ID;

namespace tsorcRevamp.NPCs
{
    public partial class tsorcRevampGlobalNPC
    {
        // This is a movement posture, not a cadence/guard-pressure stack. Repeated distant hits refresh a bounded
        // pursuit window. Every kiter gets the baseline close-in response; selected ranged fighters may opt into
        // AdvanceAndShoot to pursue faster without pausing their authored ranged scheduler.
        public const int KiteRangedThreatPursuitTicks = 240;
        private const int KiteRangedThreatRefreshTicks = 60;
        private const float MinimumCloseThreatRangeTiles = 3f;

        public int KiteRangedThreatTimer { get; private set; }
        public int KiteRangedThreatPlayer { get; private set; } = -1;
        public bool CanAdvanceAndShoot;
        // Presentation capability, intentionally false by default. Enemies with fixed aim/fire frames but no
        // walk-while-firing frames can still use AdvanceAndShoot to close quickly between shots without sliding.
        public bool CanFireWhileAdvancing;
        public float AdvanceAndShootSpeedMultiplier = 1.45f;
        public float AdvanceAndShootAccelerationMultiplier = 1.35f;

        /// <summary>
        /// True when this kiting enemy is temporarily pursuing its current target in response to damage delivered
        /// from beyond its close-threat band. The movement layer treats this as an effective kite minimum of zero.
        /// </summary>
        internal bool IsClosingOnRangedThreat(NPC npc, Player target)
        {
            return KiteRangeMax > 0f
                && KiteRangedThreatTimer > 0
                && KiteRangedThreatPlayer == target.whoAmI
                && npc.target == target.whoAmI
                && target.active
                && !target.dead;
        }

        internal bool IsAdvanceAndShootActive(NPC npc, Player target)
        {
            return CanAdvanceAndShoot && IsClosingOnRangedThreat(npc, target);
        }

        internal bool CanUseMovingFireDuringAdvance(NPC npc, Player target)
        {
            return CanFireWhileAdvancing && IsAdvanceAndShootActive(npc, target);
        }

        /// <summary>
        /// Direct item hits necessarily came from close range, so they restore the enemy's authored anti-melee
        /// kite band. Only the current target can change that enemy's movement posture.
        /// </summary>
        internal void RegisterKiteThreatFromItem(NPC npc, Player player)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient || KiteRangeMax <= 0f
                || player == null || !player.active || player.dead || npc.target != player.whoAmI)
            {
                return;
            }

            ClearKiteRangedThreat(npc);
        }

        /// <summary>
        /// Registers a spatially-ranged projectile hit. Delivery distance is used instead of DamageClass so melee
        /// beams, magic, summons, and other projectile weapons all receive the same positioning counterplay, while
        /// close weapon hitboxes and whips still reinforce the ordinary anti-melee kite posture.
        /// </summary>
        /// <returns>True when this hit represents a distant threat and should also break standing-fire pauses.</returns>
        internal bool RegisterKiteThreatFromProjectile(NPC npc, Projectile projectile)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient || KiteRangeMax <= 0f || !projectile.friendly
                || projectile.owner < 0 || projectile.owner >= Main.maxPlayers)
            {
                return false;
            }

            Player player = Main.player[projectile.owner];
            if (!player.active || player.dead || npc.target != player.whoAmI)
            {
                return false;
            }

            float closeThreatRangeTiles = Math.Max(MinimumCloseThreatRangeTiles, KiteRangeMin);
            float sourceDistanceTiles = npc.Distance(player.Center) / 16f;
            if (sourceDistanceTiles <= closeThreatRangeTiles)
            {
                ClearKiteRangedThreat(npc);
                return false;
            }

            bool changedTarget = KiteRangedThreatPlayer != player.whoAmI;
            bool needsRefresh = KiteRangedThreatTimer <= KiteRangedThreatPursuitTicks - KiteRangedThreatRefreshTicks;
            if (changedTarget || KiteRangedThreatTimer <= 0 || needsRefresh)
            {
                KiteRangedThreatPlayer = player.whoAmI;
                KiteRangedThreatTimer = KiteRangedThreatPursuitTicks;
                npc.netUpdate = true;
            }

            return true;
        }

        private void UpdateKiteThreatResponse(NPC npc)
        {
            if (KiteRangedThreatTimer <= 0)
            {
                return;
            }

            bool sourceValid = KiteRangedThreatPlayer >= 0
                && KiteRangedThreatPlayer < Main.maxPlayers
                && Main.player[KiteRangedThreatPlayer].active
                && !Main.player[KiteRangedThreatPlayer].dead;
            if (KiteRangeMax <= 0f || !sourceValid)
            {
                ClearKiteRangedThreat(npc);
                return;
            }

            KiteRangedThreatTimer--;
            if (KiteRangedThreatTimer == 0)
            {
                KiteRangedThreatPlayer = -1;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    npc.netUpdate = true;
                }
            }
        }

        private void ClearKiteRangedThreat(NPC npc)
        {
            bool wasActive = KiteRangedThreatTimer > 0 || KiteRangedThreatPlayer != -1;
            KiteRangedThreatTimer = 0;
            KiteRangedThreatPlayer = -1;
            if (wasActive && Main.netMode != NetmodeID.MultiplayerClient)
            {
                npc.netUpdate = true;
            }
        }

        internal void SendKiteThreat(BinaryWriter writer)
        {
            writer.Write(KiteRangedThreatTimer);
            writer.Write(KiteRangedThreatPlayer);
        }

        internal void ReceiveKiteThreat(BinaryReader reader)
        {
            KiteRangedThreatTimer = reader.ReadInt32();
            KiteRangedThreatPlayer = reader.ReadInt32();
        }
    }
}
