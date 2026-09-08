using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace tsorcRevamp.Projectiles.Enemy.Weapons
{
    /// <summary>
    /// Dread Wraith's Flaming Mace, rigged as a ball-and-chain flail (see EnemyFlailProjectileBase)
    /// instead of the vanilla item's held swing — same "signature weapon" treatment GreatBlackKnight's
    /// flail gets. PLACEHOLDER sprite (copy of DiamondCrusherBall/Chain) — replace with a bespoke
    /// fiery mace-head sprite; the ember trail below is standing in for the missing fire coloring.
    /// </summary>
    public class DreadWraithMaceBall : EnemyFlailProjectileBase
    {
        protected override string ChainTexturePath => "tsorcRevamp/Projectiles/Enemy/Weapons/DreadWraithMaceChain";

        // Spin telegraph at 5 tiles, then a fast lash out to as far as 20 tiles. The out/return are kept
        // short and constant so a long lash is visibly faster than a short one — distance reads as danger.
        protected override int SpinTelegraphTicks => 42;
        protected override float SpinRadius => 80f;   // 5 tiles
        protected override int LashOutTicks => 9;
        protected override int LashReturnTicks => 18;

        // Must cover telegraph + out + return with headroom, or the head is deleted mid-lash and the chain
        // visually snaps (the bug GreatBlackKnightFlail's comments warn about).
        protected override int Lifetime => 42 + 9 + 18 + 30;

        private const int FireDebuffTicks = 5 * 60;

        protected override void OnFlailTick(NPC owner, Vector2 hand)
        {
            // Continuous flame licking off the ball every tick, since the placeholder sprite itself
            // isn't fire-colored yet — a slight upward drift so it reads as fire, not sparks.
            Dust fire = Dust.NewDustPerfect(Projectile.Center, DustID.Torch, Main.rand.NextVector2Circular(1f, 1f) - new Vector2(0f, 1f), 100, default, Main.rand.NextFloat(0.9f, 1.4f));
            fire.noGravity = true;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.OnFire, FireDebuffTicks);
        }
    }
}
