using Terraria;

namespace tsorcRevamp.NPCs
{
    /// <summary>
    /// Implemented by a ModNPC with its OWN reaction to being hit — a teleport roll, a hop away, a state-timer nudge —
    /// that lives in bespoke fields rather than in the shared <see cref="tsorcRevampAIs.FighterOnHit"/> /
    /// <see cref="tsorcRevampAIs.EvasiveOnHit"/> reactions.
    /// <para/>
    /// It exists because <c>ModNPC.OnHitBy*</c> runs ONLY on the client that dealt the hit, never on a multiplayer
    /// server, so a reaction written directly in those hooks either does nothing in MP (the teleport helpers are
    /// server-gated internally) or moves the enemy on one client until the next position sync undoes it. The enemy
    /// calls <see cref="tsorcRevampGlobalNPC.RequestHitReaction"/> from its hook instead: in singleplayer and on the
    /// server that runs <see cref="OnServerHit"/> straight away, and on a client it rides that hit's NPCHitReport to
    /// the server, which calls it from ApplyHitReport. Rolls and spawns therefore happen exactly once, where they are
    /// authoritative.
    /// </summary>
    public interface IHitReactor
    {
        /// <summary>Server/singleplayer only: this enemy's own response to one hit. Safe to roll Main.rand, set
        /// velocity, and queue teleports here. <paramref name="melee"/> distinguishes the melee and ranged reactions
        /// the OnHitByItem / OnHitByProjectile hooks used to hold separately.</summary>
        void OnServerHit(NPC npc, bool melee);
    }
}
