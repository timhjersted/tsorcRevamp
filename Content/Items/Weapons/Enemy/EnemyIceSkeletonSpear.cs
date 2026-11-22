using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Weapons.Enemy
{
    /// <summary>Presentation-only spear the Ice Skeleton conjures for its attacks. Deliberately shares the 128x128
    /// IceSkeletonSpear art (permanent texture sharing, no class owns that PNG). Head (4, 2) upper-left, butt
    /// (124, 126) lower-right.</summary>
    public class EnemyIceSkeletonSpear : ModItem
    {
        public const string ArtPath = "tsorcRevamp/Content/Projectiles/Enemy/IceSkeletonSpear";

        public override string Texture => ArtPath;

        public override void SetDefaults()
        {
            Item.width = 128;
            Item.height = 128;
            Item.damage = 1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useAnimation = 44;
            Item.useTime = 44;
            Item.DamageType = DamageClass.Melee;
        }
    }
}
