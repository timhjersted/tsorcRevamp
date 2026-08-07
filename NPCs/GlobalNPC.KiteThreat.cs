using System;
using System.IO;
using Terraria;
using Terraria.ID;

namespace tsorcRevamp.NPCs
{
    public partial class tsorcRevampGlobalNPC
    {
        // Movement posture for ranged kiters. All enemies with KiteRangeMax > 0 are AGGRESSIVE by default
        // (they close in to melee). A melee hit opens a timed kite window — the enemy backs off into its
        // preferred band for KiteRangedThreatPursuitTicks (~4s), then returns to aggression. A projectile
        // hit while in the kite window immediately clears it, snapping the enemy back to pursuit.
        // This "fight until punched, then back off" rhythm rewards melee pressure: players who stay in
        // melee keep the enemy pursuing (close and predictable); players who retreat to range give the
        // enemy room to kite comfortably.
        public const int KiteRangedThreatPursuitTicks = 240; // kite-window duration after a melee hit

        private const int KiteRangedThreatRefreshTicks = 60;
        private const float MinimumCloseThreatRangeTiles = 3f;

        // When > 0, the enemy is in a kite window (backing off after being hit by a melee weapon).
        // When == 0, the enemy is in its default aggressive posture (pursuing).
        public int KiteRangedThreatTimer { get; private set; }
        public int KiteRangedThreatPlayer { get; private set; } = -1;
        public bool CanAdvanceAndShoot;
        // Presentation capability, intentionally false by default. Enemies with fixed aim/fire frames but no
        // walk-while-firing frames can still use AdvanceAndShoot to close quickly between shots without sliding.
        public bool CanFireWhileAdvancing;
        public float AdvanceAndShootSpeedMultiplier = 1.45f;
        public float AdvanceAndShootAccelerationMultiplier = 1.35f;

        /// <summary>
        /// True when this kiting enemy is in its default aggressive posture — pursuing the player rather
        /// than maintaining the kite band. This is the DEFAULT state. It is only false when the enemy is
        /// in a melee-hit kite window (KiteRangedThreatTimer > 0).
        /// </summary>
        internal bool IsClosingOnRangedThreat(NPC npc, Player target)
        {
            return KiteRangeMax > 0f
                && KiteRangedThreatTimer == 0   // timer == 0 → aggressive (no active kite window)
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
        /// A melee item hit: opens a kite window so the enemy backs off into its preferred band.
        /// Only the current target can change this enemy's movement posture.
        /// </summary>
        internal void RegisterKiteThreatFromItem(NPC npc, Player player)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient || KiteRangeMax <= 0f
                || player == null || !player.active || player.dead || npc.target != player.whoAmI)
            {
                return;
            }

            // Melee hit → open kite window (back off into band for ~4s).
            bool changedTarget = KiteRangedThreatPlayer != player.whoAmI;
            bool needsRefresh = KiteRangedThreatTimer <= KiteRangedThreatPursuitTicks - KiteRangedThreatRefreshTicks;
            if (changedTarget || KiteRangedThreatTimer <= 0 || needsRefresh)
            {
                KiteRangedThreatPlayer = player.whoAmI;
                KiteRangedThreatTimer = KiteRangedThreatPursuitTicks;
                npc.netUpdate = true;
            }
        }

        /// <summary>
        /// A projectile hit: clears any active kite window so the enemy immediately returns to its
        /// default aggressive posture (pursuing). Distance filtering still applies — close-range
        /// projectiles (within the kite band) do NOT clear the window, so the enemy can still back off
        /// from a point-blank shot.
        /// </summary>
        /// <returns>True when this hit clears the kite window and returns the enemy to aggression.</returns>
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

            // A close-range projectile (within the kite band) doesn't snap the enemy back to aggression —
            // the player is already in melee range and the enemy should still back off.
            float closeThreatRangeTiles = Math.Max(MinimumCloseThreatRangeTiles, KiteRangeMin);
            float sourceDistanceTiles = npc.Distance(player.Center) / 16f;
            if (sourceDistanceTiles <= closeThreatRangeTiles)
            {
                return false;
            }

            // Distant projectile hit → cancel the kite window, snap back to aggressive pursuit.
            if (KiteRangedThreatTimer > 0)
            {
                ClearKiteRangedThreat(npc);
                return true;
            }

            return false;
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
