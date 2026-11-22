using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Projectiles.Enemy;

namespace tsorcRevamp.Content.Items.Weapons.Enemy
{
    /// <summary>Presentation-only knife the Tibian Amazon holds in each hand and throws. Deliberately shares the
    /// 18x34 EnemyThrowingKnife art (permanent texture sharing, not a placeholder), so the knife in her fist and the
    /// one in flight are the same sprite. The art stands point-up: tip (8.5, 0), crossguard y 18-21, grip centre
    /// (9, 28.5), pommel (8.5, 34).</summary>
    public class EnemyTibianAmazonKnife : ModItem
    {
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(EnemyThrowingKnife));

        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 34;
            Item.damage = 1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useAnimation = 20;
            Item.useTime = 20;
            Item.DamageType = DamageClass.Melee;
        }
    }
}
