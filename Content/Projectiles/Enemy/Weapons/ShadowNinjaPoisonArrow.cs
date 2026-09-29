using Terraria;
using Terraria.ID;

namespace tsorcRevamp.Content.Projectiles.Enemy.Weapons
{
    public class ShadowNinjaPoisonArrow : EnemyTaintedArrow
    {
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.Poisoned, 6 * 60);
        }
    }
}
