using tsorcRevamp.Content.Projectiles.Melee.Flails;

namespace tsorcRevamp.Content.Projectiles.Enemy.Weapons
{
    public class EnemyDiamondCrusherBall : EnemyFlailProjectileBase
    {
        protected override string ChainTexturePath => UsefulFunctions.RefactorableFilepath(typeof(DiamondCrusherBall)) + "_Chain";

        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(DiamondCrusherBall));
    }
}
