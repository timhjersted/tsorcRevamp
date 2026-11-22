using Microsoft.Xna.Framework;

namespace tsorcRevamp.NPCs
{
    /// <summary>
    /// An NPC that wields an <see cref="Content.Projectiles.Enemy.Weapons.EnemyWhipProjectileBase"/> whip. The whip
    /// projectile reads these every tick on every machine; the owner must derive them from synced state so all peers
    /// build the same rope (the Dark Blood Knight locks its aim during the tell and syncs the lock).
    /// </summary>
    public interface IEnemyWhipOwner
    {
        /// <summary>World point the handle is held at (normally the front hand).</summary>
        Vector2 WhipAnchor { get; }

        /// <summary>Unit direction the lash is thrown along.</summary>
        Vector2 WhipAim { get; }

        /// <summary>False ends the whip at once (attack cancelled, owner staggered, phase changed).</summary>
        bool IsWhipActive { get; }
    }
}
