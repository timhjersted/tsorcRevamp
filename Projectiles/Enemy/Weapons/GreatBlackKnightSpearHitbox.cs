using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using tsorcRevamp.NPCs;
using tsorcRevamp.NPCs.Enemies;

namespace tsorcRevamp.Projectiles.Enemy.Weapons
{
    /// <summary>
    /// Invisible, source-anchored hitbox for the Great Black Knight's spear jab.
    /// </summary>
    /// <remarks>
    /// Deliberately NOT <see cref="HumanoidMeleeHitbox"/>: that one is hard-coupled to the shared
    /// humanoid melee system — it kills itself unless the source has <c>CombatMeleeActive</c> and
    /// only damages during <c>InCombatMeleeHitWindow</c>. The Great Black Knight runs its own
    /// Phase/AttackKind state machine instead (wiring up the shared system as well would give one
    /// NPC two independent attack selectors racing to start attacks), so it needs a hitbox gated on
    /// that state machine. Behaviour otherwise mirrors its sibling.
    /// </remarks>
    public class GreatBlackKnightSpearHitbox : ModProjectile
    {
        // Overlap back toward the wielder so a visible fighter reaches the shield before rebounding,
        // matching HumanoidMeleeHitbox.
        private const float SourceOverlap = 22f;

        public override string Texture => "tsorcRevamp/NPCs/Puppets/PuppetPlaceholder";

        public override void SetDefaults()
        {
            Projectile.width = 64;
            Projectile.height = 44;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 60;
            Projectile.tileCollide = false;
            Projectile.alpha = 255;
            Projectile.hide = true;
            Projectile.ignoreWater = true;
        }

        public override void OnSpawn(IEntitySource source)
        {
            Vector2 center = Projectile.Center;
            Projectile.width = (int)System.Math.Max(1f, Projectile.ai[0]);
            Projectile.height = (int)System.Math.Max(1f, Projectile.ai[1]);
            Projectile.Center = center;
        }

        public override bool ShouldUpdatePosition() => false;

        private bool TryGetKnight(out NPC sourceNPC, out GreatBlackKnight knight)
        {
            knight = null;
            tsorcGlobalProjectile globalProjectile = Projectile.GetGlobalProjectile<tsorcGlobalProjectile>();
            if (!globalProjectile.TryGetSourceNPC(out sourceNPC))
            {
                return false;
            }
            knight = sourceNPC.ModNPC as GreatBlackKnight;
            return knight != null;
        }

        public override void AI()
        {
            if (!TryGetKnight(out NPC sourceNPC, out GreatBlackKnight knight) || !knight.SpearMeleeActive)
            {
                Projectile.Kill();
                return;
            }

            int direction = Projectile.velocity.X < 0f ? -1 : 1;
            Projectile.Center = sourceNPC.Center + new Vector2(direction * (Projectile.width * 0.5f - SourceOverlap), -4f);
            Projectile.timeLeft = 2;
        }

        public override bool? CanDamage()
        {
            return TryGetKnight(out _, out GreatBlackKnight knight) && knight.SpearMeleeHitWindow;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            if (TryGetKnight(out NPC sourceNPC, out _) && sourceNPC.ModNPC is IHumanoidMeleeHitEffects hitEffects)
            {
                hitEffects.OnHumanoidMeleeHit(target);
            }
        }
    }
}
