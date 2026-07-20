using Microsoft.Xna.Framework;

namespace tsorcRevamp.NPCs
{
    /// <summary>
    /// Implemented by a ModNPC that wants a ball-and-chain flail projectile (see
    /// Projectiles.Enemy.Weapons.EnemyFlailProjectileBase) to anchor its chain to a specific rigged point — e.g. a
    /// walk-cycle hand position — instead of the generic Center+offset guess the base class falls back to for
    /// owners that don't implement this. Only the anchor point is needed; the flail already reads the owner NPC's
    /// own `direction` for its spin/windup math.
    /// </summary>
    public interface IFlailAnchor
    {
        Vector2 GetFlailAnchor();
    }
}
